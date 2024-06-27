using CodexOpenApi;
using CodexPlugin;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace AutoClient
{
    public class Runner
    {
        private readonly ILog log;
        private readonly HttpClient client;
        private readonly Address address;
        private readonly CodexApi codex;
        private readonly CancellationToken ct;
        private readonly Configuration config;
        private readonly ImageGenerator generator;

        public Runner(ILog log, HttpClient client, Address address, CodexApi codex, CancellationToken ct, Configuration config, ImageGenerator generator)
        {
            this.log = log;
            this.client = client;
            this.address = address;
            this.codex = codex;
            this.ct = ct;
            this.config = config;
            this.generator = generator;
        }

        public async Task Run()
        {
            while (!ct.IsCancellationRequested)
            {
                log.Log("New run!");

                try
                {
                    await DoRun();

                    log.Log("Run succcessful.");
                }
                catch (Exception ex)
                {
                    log.Error("Exception during run: " + ex);
                }

                await FixedShortDelay();
            }
        }

        private async Task DoRun()
        {
            var file = await CreateFile();
            var cid = await UploadFile(file);
            var pid = await RequestStorage(cid);
            await WaitUntilStarted(pid);
        }

        private async Task<string> CreateFile()
        {
            return await generator.GenerateImage();
        }

        private async Task<ContentId> UploadFile(string filename)
        {
            // Copied from CodexNode :/
            using var fileStream = File.OpenRead(filename);

            var logMessage = $"Uploading file {filename}...";
            log.Log(logMessage);
            var response = await codex.UploadAsync(fileStream, ct);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith("Unable to store block")) FrameworkAssert.Fail("Node failed to store block.");

            log.Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        private async Task<string> RequestStorage(ContentId cid)
        {
            log.Log("Requesting storage for " + cid.Id);
            var result = await codex.CreateStorageRequestAsync(cid.Id, new StorageRequestCreation()
            {
                Collateral = config.RequiredCollateral.ToString(),
                Duration = (config.ContractDurationMinutes * 60).ToString(),
                Expiry = (config.ContractExpiryMinutes * 60).ToString(),
                Nodes = config.NumHosts,
                Reward = config.Price.ToString(),
                ProofProbability = "15",
                Tolerance = config.HostTolerance
            }, ct);

            log.Log("Response: " + result);

            return result;
        }

        private async Task<string?> GetPurchaseState(string pid)
        {
            try
            {
                // openapi still don't match code.
                var str = await client.GetStringAsync($"{address.Host}:{address.Port}/api/codex/v1/storage/purchases/{pid}");
                if (string.IsNullOrEmpty(str)) return null;
                var sp = JsonConvert.DeserializeObject<StoragePurchase>(str)!;
                log.Log($"Purchase {pid} is {sp.State}");
                if (!string.IsNullOrEmpty(sp.Error)) log.Log($"Purchase {pid} error is {sp.Error}");
                return sp.State;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task WaitUntilStarted(string pid)
        {
            log.Log("Waiting till contract is started, or expired...");
            try
            {
                var emptyResponseTolerance = 10;
                while (true)
                {
                    await FixedShortDelay();
                    var status = await GetPurchaseState(pid); 
                    if (string.IsNullOrEmpty(status))
                    {
                        emptyResponseTolerance--;
                        if (emptyResponseTolerance == 0)
                        {
                            log.Log("Received 10 empty responses. Applying expiry delay, then carrying on.");
                            await ExpiryTimeDelay();
                            return;
                        }
                        await FixedShortDelay();
                    }
                    else
                    {
                        if (status.Contains("pending") || status.Contains("submitted"))
                        {
                            await FixedShortDelay();
                        }
                        else if (status.Contains("started"))
                        {
                            log.Log("Started.");
                            await FixedDurationDelay();
                        }
                        else if (status.Contains("finished"))
                        {
                            log.Log("Purchase finished.");
                            return;
                        }
                        else if (status.Contains("error"))
                        {
                            await FixedShortDelay();
                            return;
                        }
                        else
                        {
                            await FixedShortDelay();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log($"Wait failed with exception: {ex}. Assume contract will expire: Wait expiry time.");
                await ExpiryTimeDelay();
            }
        }

        private async Task FixedDurationDelay()
        {
            await Task.Delay(config.ContractDurationMinutes * 60 * 1000);
        }

        private async Task ExpiryTimeDelay()
        {
            await Task.Delay(config.ContractExpiryMinutes * 60 * 1000);
        }

        private async Task FixedShortDelay()
        {
            await Task.Delay(15 * 1000);
        }
    }
}
