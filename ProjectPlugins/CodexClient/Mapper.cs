﻿using Newtonsoft.Json.Linq;
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

        public LocalDatasetList Map(LocalDatasetListJson json)
        {
            return new LocalDatasetList
            {
                Content = json.Content.Select(Map).ToArray()
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

        public LocalDataset Map(LocalDatasetListJsonItem item)
        {
            return new LocalDataset
            {
                Cid = new ContentId(item.Cid),
                Manifest = MapManifest(item.Manifest)
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

        // TODO: Fix openapi spec for this call.
        //public StoragePurchase Map(CodexOpenApi.Purchase purchase)
        //{
        //    return new StoragePurchase(Map(purchase.Request))
        //    {
        //        State = purchase.State,
        //        Error = purchase.Error
        //    };
        //}

        //public StorageRequest Map(CodexOpenApi.StorageRequest request)
        //{
        //    return new StorageRequest(Map(request.Ask), Map(request.Content))
        //    {
        //        Id = request.Id,
        //        Client = request.Client,
        //        Expiry = TimeSpan.FromSeconds(Convert.ToInt64(request.Expiry)),
        //        Nonce = request.Nonce
        //    };
        //}

        //public StorageAsk Map(CodexOpenApi.StorageAsk ask)
        //{
        //    return new StorageAsk
        //    {
        //        Duration = TimeSpan.FromSeconds(Convert.ToInt64(ask.Duration)),
        //        MaxSlotLoss = ask.MaxSlotLoss,
        //        ProofProbability = ask.ProofProbability,
        //        Reward = Convert.ToDecimal(ask.Reward).TstWei(),
        //        Slots = ask.Slots,
        //        SlotSize = new ByteSize(Convert.ToInt64(ask.SlotSize))
        //    };
        //}

        //public StorageContent Map(CodexOpenApi.Content content)
        //{
        //    return new StorageContent
        //    {
        //        Cid = content.Cid
        //    };
        //}

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

        public Manifest MapManifest(LocalDatasetListJsonItemManifest manifest)
        {
            return new Manifest
            {
                // needs update
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


//"content": [
//        {
//            "cid": "zDvZRwzkxLxVaGces3kpkHjo8EcTPXudvYMfNxdoH21Ask1Js5fJ",
//            "manifest": {
//                "treeCid": "zDzSvJTf8GBRyEDNuAzXS9VnRfh8cNuYuRPwTLW6RUQReSgKnhCt",
//                "datasetSize": 5242880,
//                "blockSize": 65536,
//                "filename": null,
//                "mimetype": "application/octet-stream",
//                "uploadedAt": 1731426230,
//                "protected": false
//            }
//        }
//    ]

    public class LocalDatasetListJson
    {
        public LocalDatasetListJsonItem[] Content { get; set; } = Array.Empty<LocalDatasetListJsonItem>();
    }

    public class LocalDatasetListJsonItem
    {
        public string Cid { get; set; } = string.Empty;
        public LocalDatasetListJsonItemManifest Manifest { get; set; } = new();
    }

    public class LocalDatasetListJsonItemManifest
    {
        public string TreeCid { get; set; } = string.Empty;
        public int DatasetSize { get; set; }
        public int BlockSize { get; set; }
        public string? Filename { get; set; } = string.Empty;
        public string? MimeType { get; set; } = string.Empty;
        public int? UploadedAt { get; set; } 
        public bool Protected { get; set; }
    }
}
