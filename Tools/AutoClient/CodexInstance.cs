using CodexOpenApi;
using CodexPlugin;
using Logging;
using Nethereum.Model;
using Newtonsoft.Json;
using Utils;

namespace AutoClient
{
    public interface ICodexInstance
    {
        string NodeId { get; }
        App App { get; }
        CodexApi Codex { get; }
        HttpClient Client { get; }
        Address Address { get; }
    }

    public class CodexInstance : ICodexInstance
    {
        public CodexInstance(App app, CodexApi codex, HttpClient client, Address address)
        {
            App = app;
            Codex = codex;
            Client = client;
            Address = address;
            NodeId = Guid.NewGuid().ToString();
        }

        public string NodeId { get; }
        public App App { get; }
        public CodexApi Codex { get; }
        public HttpClient Client { get; }
        public Address Address { get; }
    }

    public class CodexNode
    {
        private readonly App app;
        private readonly ICodexInstance codex;

        public CodexNode(App app, ICodexInstance instance)
        {
            this.app = app;
            codex = instance;
        }

        public async Task DownloadCid(string filename, string cid, long? size)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                using var fileStream = File.OpenWrite(filename);
                var fileResponse = await codex.Codex.DownloadNetworkStreamAsync(cid);
                fileResponse.Stream.CopyTo(fileStream);
                var time = sw.Elapsed;
                app.Performance.DownloadSuccessful(size, time);
            }
            catch (Exception ex)
            {
                app.Performance.DownloadFailed(ex);
            }
        }

        public async Task<ContentId> UploadFile(string filename)
        {
            using var fileStream = File.OpenRead(filename);
            try
            {
                var info = new FileInfo(filename);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var cid = await UploadStream(fileStream, filename);
                var time = sw.Elapsed;
                app.Performance.UploadSuccessful(info.Length, time);
                app.CidRepo.Add(codex.NodeId, cid.Id, info.Length);
                return cid;
            }
            catch (Exception exc)
            {
                app.Performance.UploadFailed(exc);
                throw;
            }
        }

        public async Task<RequestStorageResult> RequestStorage(ContentId cid)
        {
            app.Log.Debug("Requesting storage for " + cid.Id);
            var result = await codex.Codex.CreateStorageRequestAsync(cid.Id, new StorageRequestCreation()
            {
                Collateral = app.Config.RequiredCollateral.ToString(),
                Duration = (app.Config.ContractDurationMinutes * 60).ToString(),
                Expiry = (app.Config.ContractExpiryMinutes * 60).ToString(),
                Nodes = app.Config.NumHosts,
                Reward = app.Config.Price.ToString(),
                ProofProbability = "15",
                Tolerance = app.Config.HostTolerance
            }, app.Cts.Token);

            app.Log.Debug("Purchase ID: " + result);

            var encoded = await GetEncodedCid(result);
            app.CidRepo.AddEncoded(cid.Id, encoded);

            return new RequestStorageResult(result, new ContentId(encoded));
        }

        public class RequestStorageResult
        {
            public RequestStorageResult(string purchaseId, ContentId encodedCid)
            {
                PurchaseId = purchaseId;
                EncodedCid = encodedCid;
            }

            public string PurchaseId { get; }
            public ContentId EncodedCid { get; }

            public override string ToString()
            {
                return $"{PurchaseId} (cid: {EncodedCid})";
            }
        }

        public async Task<StoragePurchase?> GetStoragePurchase(string pid)
        {
            // openapi still don't match code.
            var str = await codex.Client.GetStringAsync($"{codex.Address.Host}:{codex.Address.Port}/api/codex/v1/storage/purchases/{pid}");
            if (string.IsNullOrEmpty(str)) return null;
            return JsonConvert.DeserializeObject<StoragePurchase>(str);
        }

        private async Task<ContentId> UploadStream(FileStream fileStream, string filename)
        {
            app.Log.Debug($"Uploading file...");
            var response = await codex.Codex.UploadAsync(
                content_type: "application/octet-stream",
                content_disposition: $"attachment; filename=\"{filename}\"",
                fileStream, app.Cts.Token);

            if (string.IsNullOrEmpty(response)) FrameworkAssert.Fail("Received empty response.");
            if (response.StartsWith("Unable to store block")) FrameworkAssert.Fail("Node failed to store block.");

            app.Log.Debug($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        private async Task<string> GetEncodedCid(string pid)
        {
            try
            {
                var sp = (await GetStoragePurchase(pid))!;
                return sp.Request.Content.Cid;
            }
            catch (Exception ex)
            {
                app.Log.Error(ex.ToString());
                throw;
            }
        }
    }
}
