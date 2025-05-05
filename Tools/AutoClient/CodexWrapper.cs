using CodexClient;
using FileUtils;
using Utils;

namespace AutoClient
{
    public class CodexWrapper
    {
        private readonly App app;
        private static readonly Random r = new Random();

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
            var (nodes, tolerance) = GetDurability();
            var result = Node.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                CollateralPerByte = app.Config.CollateralPerByte.TstWei(),
                Duration = GetDuration(),
                Expiry = TimeSpan.FromMinutes(app.Config.ContractExpiryMinutes),
                MinRequiredNumberOfNodes = nodes,
                NodeFailureTolerance = tolerance,
                PricePerBytePerSecond = GetPricePerBytePerSecond(),
                ProofProbability = GetProofProbability()
            });
            return result;
        }

        public StoragePurchase? GetStoragePurchase(string pid)
        {
            return Node.GetPurchaseStatus(pid);
        }

        private TestToken GetPricePerBytePerSecond()
        {
            var i = app.Config.PricePerBytePerSecond;
            i -= 100;
            i += r.Next(0, 1000);

            return i.TstWei();
        }

        private TimeSpan GetDuration()
        {
            var i = app.Config.ContractDurationMinutes;
            var day = 60 * 24;
            i -= day;
            i -= 10; // We don't want to accidentally hit exactly 7 days because that's the limit of the storage node availabilities.
            i += r.Next(0, day * 2);

            return TimeSpan.FromMinutes(i);
        }

        private (uint, uint) GetDurability()
        {
            (uint, uint)[] options = [
                (4, 2),
                (5, 2),
                (10, 5),
                (20, 10)
            ];

            return options[r.Next(0, options.Length)];
        }

        private int GetProofProbability()
        {
            return r.Next(10, 100);
        }
    }
}
