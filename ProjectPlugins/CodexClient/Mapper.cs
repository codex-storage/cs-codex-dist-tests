using CodexOpenApi;
using Newtonsoft.Json.Linq;
using System.Numerics;
using Utils;

namespace CodexClient
{
    public class Mapper
    {
        public DebugInfo Map(CodexOpenApi.DebugInfo debugInfo)
        {
            return new DebugInfo
            {
                Id = debugInfo.Id,
                Spr = debugInfo.Spr,
                Addrs = debugInfo.Addrs.ToArray(),
                AnnounceAddresses = debugInfo.AnnounceAddresses.ToArray(),
                Version = Map(debugInfo.Codex),
                Table = Map(debugInfo.Table)
            };
        }

        public LocalDatasetList Map(CodexOpenApi.DataList dataList)
        {
            return new LocalDatasetList
            {
                Content = dataList.Content.Select(Map).ToArray()
            };
        }

        public LocalDataset Map(CodexOpenApi.DataItem dataItem)
        {
            return new LocalDataset
            {
                Cid = new ContentId(dataItem.Cid),
                Manifest = MapManifest(dataItem.Manifest)
            };
        }

        public CodexOpenApi.SalesAvailabilityCREATE Map(StorageAvailability availability)
        {
            return new CodexOpenApi.SalesAvailabilityCREATE
            {
                Duration = ToDecInt(availability.MaxDuration.TotalSeconds),
                MinPricePerBytePerSecond = ToDecInt(availability.MinPricePerBytePerSecond),
                TotalCollateral = ToDecInt(availability.TotalCollateral),
                TotalSize = ToDecInt(availability.TotalSpace.SizeInBytes)
            };
        }

        public CodexOpenApi.StorageRequestCreation Map(StoragePurchaseRequest purchase)
        {
            return new CodexOpenApi.StorageRequestCreation
            {
                Duration = ToDecInt(purchase.Duration.TotalSeconds),
                ProofProbability = ToDecInt(purchase.ProofProbability),
                PricePerBytePerSecond = ToDecInt(purchase.PricePerBytePerSecond),
                CollateralPerByte = ToDecInt(purchase.CollateralPerByte),
                Expiry = ToDecInt(purchase.Expiry.TotalSeconds),
                Nodes = Convert.ToInt32(purchase.MinRequiredNumberOfNodes),
                Tolerance = Convert.ToInt32(purchase.NodeFailureTolerance)
            };
        }

        public StorageAvailability[] Map(ICollection<CodexOpenApi.SalesAvailabilityREAD> availabilities)
        {
            return availabilities.Select(a => Map(a)).ToArray();
        }

        public StorageAvailability Map(CodexOpenApi.SalesAvailabilityREAD availability)
        {
            return new StorageAvailability
            (
                ToByteSize(availability.TotalSize),
                ToTimespan(availability.Duration),
                new TestToken(ToBigIng(availability.MinPricePerBytePerSecond)),
                new TestToken(ToBigIng(availability.TotalCollateral))
            )
            {
                Id = availability.Id,
                FreeSpace = ToByteSize(availability.FreeSize),
            };
        }

        public StoragePurchase Map(CodexOpenApi.Purchase purchase)
        {
            return new StoragePurchase
            {
                Request = Map(purchase.Request),
                State = purchase.State.ToString(), //Map(purchase.State),
                Error = purchase.Error
            };
        }

        public StoragePurchaseState Map(PurchaseState purchaseState)
        {
            // TODO: to be re-enabled when marketplace api lines up with openapi.yaml.

            // Explicit mapping: If the API changes, we will get compile errors here.
            // That's what we want.
            switch (purchaseState)
            {
                case PurchaseState.Cancelled:
                    return StoragePurchaseState.Cancelled;
                case PurchaseState.Error:
                    return StoragePurchaseState.Error;
                case PurchaseState.Failed:
                    return StoragePurchaseState.Failed;
                case PurchaseState.Finished:
                    return StoragePurchaseState.Finished;
                case PurchaseState.Pending:
                    return StoragePurchaseState.Pending;
                case PurchaseState.Started:
                    return StoragePurchaseState.Started;
                case PurchaseState.Submitted:
                    return StoragePurchaseState.Submitted;
                case PurchaseState.Unknown:
                    return StoragePurchaseState.Unknown;
            }

            throw new Exception("API incompatibility detected. Unknown purchaseState: " + purchaseState.ToString());
        }

        public StorageRequest Map(CodexOpenApi.StorageRequest request)
        {
            return new StorageRequest
            {
                Ask = Map(request.Ask),
                Content = Map(request.Content),
                Id = request.Id,
                Client = request.Client,
                Expiry = request.Expiry,
                Nonce = request.Nonce
            };
        }

        public StorageAsk Map(CodexOpenApi.StorageAsk ask)
        {
            return new StorageAsk
            {
                Duration = ask.Duration,
                MaxSlotLoss = ask.MaxSlotLoss,
                ProofProbability = ask.ProofProbability,
                PricePerBytePerSecond = ask.PricePerBytePerSecond,
                Slots = ask.Slots,
                SlotSize = ask.SlotSize
            };
        }

        public StorageContent Map(CodexOpenApi.Content content)
        {
            return new StorageContent
            {
                Cid = content.Cid
            };
        }

        public CodexSpace Map(CodexOpenApi.Space space)
        {
            return new CodexSpace
            {
                QuotaMaxBytes = space.QuotaMaxBytes,
                QuotaReservedBytes = space.QuotaReservedBytes,
                QuotaUsedBytes = space.QuotaUsedBytes,
                TotalBlocks = space.TotalBlocks
            };
        }

        private DebugInfoVersion Map(CodexOpenApi.CodexVersion obj)
        {
            return new DebugInfoVersion
            {
                Version = obj.Version,
                Revision = obj.Revision
            };
        }

        private DebugInfoTable Map(CodexOpenApi.PeersTable obj)
        {
            return new DebugInfoTable
            {
                LocalNode = Map(obj.LocalNode),
                Nodes = Map(obj.Nodes)
            };
        }

        private DebugInfoTableNode Map(CodexOpenApi.Node? token)
        {
            if (token == null) return new DebugInfoTableNode();
            return new DebugInfoTableNode
            {
                Address = token.Address,
                NodeId = token.NodeId,
                PeerId = token.PeerId,
                Record = token.Record,
                Seen = token.Seen
            };
        }

        private DebugInfoTableNode[] Map(ICollection<CodexOpenApi.Node> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return new DebugInfoTableNode[0];
            }

            return nodes.Select(Map).ToArray();
        }

        private Manifest MapManifest(CodexOpenApi.ManifestItem manifest)
        {
            return new Manifest
            {
                BlockSize = new ByteSize(Convert.ToInt64(manifest.BlockSize)),
                OriginalBytes = new ByteSize(Convert.ToInt64(manifest.DatasetSize)),
                RootHash = manifest.TreeCid,
                Protected = manifest.Protected
            };
        }

        private JArray JArray(IDictionary<string, object> map, string name)
        {
            return (JArray)map[name];
        }

        private JObject JObject(IDictionary<string, object> map, string name)
        {
            return (JObject)map[name];
        }

        private string StringOrEmpty(JObject obj, string name)
        {
            if (obj.TryGetValue(name, out var token))
            {
                var str = (string?)token;
                if (!string.IsNullOrEmpty(str)) return str;
            }
            return string.Empty;
        }

        private bool Bool(JObject obj, string name)
        {
            if (obj.TryGetValue(name, out var token))
            {
                return (bool)token;
            }
            return false;
        }

        private string ToDecInt(double d)
        {
            var i = new BigInteger(d);
            return i.ToString("D");
        }

        private string ToDecInt(TestToken t)
        {
            return t.TstWei.ToString("D");
        }

        private BigInteger ToBigIng(string tokens)
        {
            return BigInteger.Parse(tokens);
        }

        private TimeSpan ToTimespan(string duration)
        {
            return TimeSpan.FromSeconds(Convert.ToInt32(duration));
        }

        private ByteSize ToByteSize(string size)
        {
            return new ByteSize(Convert.ToInt64(size));
        }
    }
}
