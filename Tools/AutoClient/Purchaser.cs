using CodexOpenApi;
using CodexPlugin;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace AutoClient
{
    public class Purchaser
    {
        private readonly ILog log;
        private readonly HttpClient client;
        private readonly Address address;
        private readonly CodexApi codex;
        private readonly Configuration config;
        private readonly ImageGenerator generator;
        private readonly CancellationToken ct;

        public Purchaser(ILog log, HttpClient client, Address address, CodexApi codex, Configuration config, ImageGenerator generator, CancellationToken ct)
        {
            this.log = log;
            this.client = client;
            this.address = address;
            this.codex = codex;
            this.config = config;
            this.generator = generator;
            this.ct = ct;
        }

        public void Start()
        {
            Task.Run(Worker);
        }

        private async Task Worker()
        {
            while (!ct.IsCancellationRequested)
            {
                var pid = await StartNewPurchase();
                await WaitTillFinished(pid);
            }
        }

        private async Task<string> StartNewPurchase()
        {
            var file = await CreateFile();
            var cid = await UploadFile(file);
            return await RequestStorage(cid);
        }

        private async Task<string> CreateFile()
        {
            return await generator.GenerateImage();
        }

        private async Task<ContentId> UploadFile(string filename)
        {
            // Copied from CodexNode :/
            using var fileStream = File.OpenRead(filename);

            log.Log($"Uploading file {filename}...");
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

            log.Log("Purchase ID: " + result);

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
            catch
            {
                return null;
            }
        }

        private async Task WaitTillFinished(string pid)
        {
            log.Log("Waiting...");
            try
            {
                var emptyResponseTolerance = 10;
                while (true)
                {
                    var status = (await GetPurchaseState(pid))?.ToLowerInvariant();
                    if (string.IsNullOrEmpty(status))
                    {
                        emptyResponseTolerance--;
                        if (emptyResponseTolerance == 0)
                        {
                            log.Log("Received 10 empty responses. Stop tracking this purchase.");
                            await ExpiryTimeDelay();
                            return;
                        }
                    }
                    else
                    {
                        if (status.Contains("cancel") ||
                            status.Contains("error") ||
                            status.Contains("finished"))
                        {
                            return;
                        }
                        if (status.Contains("started"))
                        {
                            await FixedDurationDelay();
                        }
                    }

                    await FixedShortDelay();
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
            await Task.Delay(config.ContractDurationMinutes * 60 * 1000, ct);
        }

        private async Task ExpiryTimeDelay()
        {
            await Task.Delay(config.ContractExpiryMinutes * 60 * 1000, ct);
        }

        private async Task FixedShortDelay()
        {
            await Task.Delay(15 * 1000, ct);
        }
    }
}
