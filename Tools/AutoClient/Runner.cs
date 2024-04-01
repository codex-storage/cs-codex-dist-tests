using CodexContractsPlugin;
using CodexPlugin;
using FileUtils;
using Logging;
using Utils;

namespace AutoClient
{
    public class Runner
    {
        private readonly ILog log;
        private readonly Codex codex;
        private readonly IFileManager fileManager;
        private readonly CancellationToken ct;
        private readonly Configuration config;

        public Runner(ILog log, Codex codex, IFileManager fileManager, CancellationToken ct, Configuration config)
        {
            this.log = log;
            this.codex = codex;
            this.fileManager = fileManager;
            this.ct = ct;
            this.config = config;
        }

        public void Run()
        {
            while (!ct.IsCancellationRequested)
            {
                log.Log("New run!");

                try
                {
                    fileManager.ScopedFiles(() =>
                    {
                        DoRun();
                    });

                    log.Log("Run succcessful.");
                }
                catch (Exception ex)
                {
                    log.Error("Exception during run: " + ex);
                }
                
                FixedShortDelay();
            }
        }

        private void DoRun()
        {
            var file = CreateFile();
            var cid = UploadFile(file);
            var pid = RequestStorage(cid);
            WaitUntilStarted(pid);
        }

        private TrackedFile CreateFile()
        {
            return fileManager.GenerateFile(new ByteSize(Convert.ToInt64(config.DatasetSizeBytes)));
        }

        private ContentId UploadFile(TrackedFile file)
        {
            // Copied from CodexNode :/
            using var fileStream = File.OpenRead(file.Filename);

            var logMessage = $"Uploading file {file.Describe()}...";
            log.Log(logMessage);
            var response = codex.UploadFile(fileStream);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith("Unable to store block")) FrameworkAssert.Fail("Node failed to store block.");

            log.Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        private string RequestStorage(ContentId cid)
        {
            var request = new StoragePurchaseRequest(cid)
            {
                PricePerSlotPerSecond = config.Price.TestTokens(),
                RequiredCollateral = config.RequiredCollateral.TestTokens(),
                MinRequiredNumberOfNodes = Convert.ToUInt32(config.NumHosts),
                NodeFailureTolerance = Convert.ToUInt32(config.HostTolerance),
                Duration = TimeSpan.FromMinutes(config.ContractDurationMinutes),
                Expiry = TimeSpan.FromMinutes(config.ContractExpiryMinutes)
            };
            request.Log(log);
            return codex.RequestStorage(request);
        }

        private void WaitUntilStarted(string pid)
        {
            log.Log("Waiting till contract is started, or expired...");
            try
            {
                var emptyResponseTolerance = 10;
                while (true)
                {
                    FixedShortDelay();
                    var status = codex.GetPurchaseStatusRaw(pid).ToLowerInvariant();
                    log.Log($"Status response: '{status}'");
                    if (string.IsNullOrEmpty(status))
                    {
                        emptyResponseTolerance--;
                        if (emptyResponseTolerance == 0)
                        {
                            log.Log("Received 10 empty responses to '/storage/purchases/'. Applying expiry delay, then carrying on.");
                            ExpiryTimeDelay();
                            return;
                        }
                        FixedShortDelay();
                    }
                    else
                    {
                        if (status.Contains("pending") || status.Contains("submitted"))
                        {
                            FixedShortDelay();
                        }
                        else
                        {
                            log.Log("Wait finished.");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Log($"Wait failed with exception: {ex}. Assume contract will expire: Wait expiry time.");
                ExpiryTimeDelay();
            }
        }

        private void ExpiryTimeDelay()
        {
            Thread.Sleep(config.ContractExpiryMinutes * 60 * 1000);
        }

        private void FixedShortDelay()
        {
            Thread.Sleep(15 * 1000);
        }
    }
}
