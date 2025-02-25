using CodexClient;
using FileUtils;
using Utils;

namespace AutoClient
{
    public class CodexWrapper
    {
        private readonly App app;

        public CodexWrapper(App app, ICodexNode node)
        {
            this.app = app;
            Node = node;
        }

        public ICodexNode Node { get; }

        public ContentId UploadFile(string filepath)
        {
            return Node.UploadFile(TrackedFile.FromPath(app.Log, filepath));
        }

        public IStoragePurchaseContract RequestStorage(ContentId cid)
        {
            app.Log.Debug("Requesting storage for " + cid.Id);
            var result = Node.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                CollateralPerByte = app.Config.CollateralPerByte.TstWei(),
                Duration = TimeSpan.FromMinutes(app.Config.ContractDurationMinutes),
                Expiry = TimeSpan.FromMinutes(app.Config.ContractExpiryMinutes),
                MinRequiredNumberOfNodes = Convert.ToUInt32(app.Config.NumHosts),
                NodeFailureTolerance = Convert.ToUInt32(app.Config.HostTolerance),
                PricePerBytePerSecond = app.Config.PricePerBytePerSecond.TstWei(),
                ProofProbability = 15
            });
            return result;
        }

        public StoragePurchase? GetStoragePurchase(string pid)
        {
            return Node.GetPurchaseStatus(pid);
        }
    }
}
