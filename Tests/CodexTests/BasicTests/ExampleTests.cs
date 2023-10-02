using CodexContractsPlugin;
using DistTestCore;
using GethPlugin;
using MetricsPlugin;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Newtonsoft.Json;
using NUnit.Framework;
using Utils;

namespace Tests.BasicTests
{
    [TestFixture]
    public class ExampleTests : CodexDistTest
    {
        [Test]
        public void AAA()
        {
            var http = this.Ci.CreateHttp();

            //var query = "{\r\n  \"query\": {\r\n    \"bool\": {\r\n      \"filter\": [\r\n        {\r\n          \"term\": {\r\n            \"container_image.keyword\": \"docker.io/codexstorage/nim-codex:sha-9d735f9-dist-tests\"\r\n          }\r\n        },\r\n        {\r\n          \"term\": {\r\n            \"pod_namespace.keyword\": \"codex-continuous-tests\"\r\n          }\r\n        },\r\n        {\r\n          \"term\": {\r\n            \"pod_name.keyword\": \"codex3-workflow3-ff476767d-98zx4\"\r\n          }\r\n        },\r\n        {\r\n          \"range\": {\r\n            \"@timestamp\": {\r\n              \"lte\": \"2023-09-25T13:02:23.559Z\",\r\n              \"gt\": \"2023-09-25T20:00:00.000Z\"\r\n            }\r\n          }\r\n        }\r\n      ]\r\n    }\r\n  }\r\n}";
            //var query = "{  \"query\": {\"bool\": {  \"filter\": [{  \"term\": {\"container_image.keyword\": \"docker.io/codexstorage/nim-codex:sha-9d735f9-dist-tests\"  }},{  \"term\": {\"pod_namespace.keyword\": \"codex-continuous-tests\"  }},{  \"term\": {\"pod_name.keyword\": \"codex3-workflow3-ff476767d-98zx4\"  }},{  \"range\": {\"@timestamp\": {  \"lte\": \"2023-09-29T13:02:23.559Z\",  \"gt\": \"2023-09-20T20:00:00.000Z\"}  }}  ]}  }}";

            

            //var queryTemplate = "{ \"query\": {\"bool\": { \"filter\": [{ \"term\": {\"pod_namespace.keyword\": \"codex-continuous-tests\" }},{ \"term\": {\"pod_name.keyword\": \"bootstrap-2-599dfb4d65-v4v2b\" }},{ \"range\": {\"@timestamp\": { \"lte\": \"2023-10-03T13:02:23.559Z\", \"gt\": \"2023-09-01T20:00:00.000Z\"} }} ]} },\t\"_source\": \"message\",\t\"from\": <FROM>,\t\"size\": <SIZE>}";
            var queryTemplate = "{ \"sort\": [ { \"@timestamp\": { \"order\": \"asc\" } } ], \"fields\": [ { \"field\": \"@timestamp\", \"format\": \"strict_date_optional_time\" }, { \"field\": \"pod_name\" }, { \"field\": \"message\" } ], \"size\": <SIZE>, <SEARCHAFTER> \"_source\": false, \"query\": { \"bool\": { \"must\": [], \"filter\": [ { \"range\": { \"@timestamp\": { \"format\": \"strict_date_optional_time\", \"gte\": \"2023-10-01T00:00:00.000Z\", \"lte\": \"2023-10-03T09:00:00.000Z\" } } }, { \"match_phrase\": { \"pod_name\": \"bootstrap-2-599dfb4d65-v4v2b\" } } ] } } }";

            var outputFile = "c:\\Users\\Ben\\Desktop\\Cluster\\reconstructed.log";

            var sizeOfPage = 2000;
            var searchAfter = "";
            var lastHits = 1;
            var totalHits = 0;
            var lastLogLine = -1;

            var queue = new List<QueryLogEntry>();
            //var jumpback = 0;

            while (lastHits > 0)
            {
                //if (queue.Any()) jumpback++;
                //else jumpback = 0;

                var query = queryTemplate
                                .Replace("<SIZE>", sizeOfPage.ToString())
                                .Replace("<SEARCHAFTER>", searchAfter);

                var output = http.HttpPostString("_search", query);

                var response = JsonConvert.DeserializeObject<SearchResponse>(output)!;

                lastHits = response.hits.hits.Length;
                totalHits += response.hits.hits.Length;
                if (lastHits > 0)
                {
                    var uniqueSearchNumbers = response.hits.hits.Select(h => h.sort.Single()).Distinct().ToList();
                    uniqueSearchNumbers.Reverse();
                    var searchNumber = uniqueSearchNumbers.Skip(1).First().ToString();
                    searchAfter = $"\"search_after\": [{searchNumber}],";

                    foreach (var hit in response.hits.hits)
                    {
                        var message = hit.fields.message.Single();
                        var sub = message.Substring(0, 12);
                        if (int.TryParse(sub, out int number))
                        {
                            queue.Add(new QueryLogEntry(message, number));
                        }
                    }
                }   

                // unload queue
                var runQueue = 1;
                while (runQueue > 0)
                {
                    var wantedNumber = lastLogLine + 1;
                    var oldOnes = queue.Where(e => e.Number < wantedNumber).ToList();
                    foreach (var old in oldOnes) queue.Remove(old);

                    var entry = queue.FirstOrDefault(e => e.Number == wantedNumber);
                    if (entry != null)
                    {
                        File.AppendAllLines(outputFile, new[] { entry.Message });
                        queue.Remove(entry);
                        lastLogLine = entry.Number;
                        if (!queue.Any()) runQueue = 0;
                    }
                    else
                    {
                        runQueue = 0;
                    }
                }
            }

            
            //var ouput = http.HttpGetString("");

            var aaa = 0;
        }

        public class QueryLogEntry
        {
            public QueryLogEntry(string message, int number)
            {
                Message = message;
                Number = number;
            }

            public string Message { get; }
            public int Number { get; }

            public override string ToString()
            {
                return Number.ToString();
            }
        }

        public class SearchResponse
        {
            public SearchHits hits { get; set; }
        }

        public class SearchHits
        {
            public SearchHitEntry[] hits { get; set; }
        }

        public class SearchHitEntry
        {
            public SearchHitFields fields { get; set; }
            public long[] sort { get; set; }
        }

        public class SearchHitFields
        {
            public string[] @timestamp { get; set; }
            public string[] message { get; set; }
        }

        [Test]
        public void CodexLogExample()
        {
            var primary = AddCodex();

            primary.UploadFile(GenerateTestFile(5.MB()));

            var log = Ci.DownloadLog(primary);

            log.AssertLogContains("Uploaded file");
        }

        [Test]
        public void TwoMetricsExample()
        {
            var group = AddCodex(2, s => s.EnableMetrics());
            var group2 = AddCodex(2, s => s.EnableMetrics());

            var primary = group[0];
            var secondary = group[1];
            var primary2 = group2[0];
            var secondary2 = group2[1];

            var metrics = Ci.GetMetricsFor(primary, primary2);

            primary.ConnectToPeer(secondary);
            primary2.ConnectToPeer(secondary2);

            Thread.Sleep(TimeSpan.FromMinutes(2));

            metrics[0].AssertThat("libp2p_peers", Is.EqualTo(1));
            metrics[1].AssertThat("libp2p_peers", Is.EqualTo(1));
        }

        [Test]
        public void MarketplaceExample()
        {
            var sellerInitialBalance = 234.TestTokens();
            var buyerInitialBalance = 1000.TestTokens();
            var fileSize = 10.MB();

            var geth = Ci.StartGethNode(s => s.IsMiner().WithName("disttest-geth"));
            var contracts = Ci.StartCodexContracts(geth);

            var seller = AddCodex(s => s
                .WithStorageQuota(11.GB())
                .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: sellerInitialBalance, isValidator: true)
                .WithSimulateProofFailures(failEveryNProofs: 3));
            
            AssertBalance(geth, contracts, seller, Is.EqualTo(sellerInitialBalance));
            seller.Marketplace.MakeStorageAvailable(
                size: 10.GB(),
                minPriceForTotalSpace: 1.TestTokens(),
                maxCollateral: 20.TestTokens(),
                maxDuration: TimeSpan.FromMinutes(3));

            var testFile = GenerateTestFile(fileSize);

            var buyer = AddCodex(s => s
                            .WithBootstrapNode(seller)
                            .EnableMarketplace(geth, contracts, initialEth: 10.Eth(), initialTokens: buyerInitialBalance));
            
            AssertBalance(geth, contracts, buyer, Is.EqualTo(buyerInitialBalance));

            var contentId = buyer.UploadFile(testFile);
            var purchaseContract = buyer.Marketplace.RequestStorage(contentId,
                pricePerSlotPerSecond: 2.TestTokens(),
                requiredCollateral: 10.TestTokens(),
                minRequiredNumberOfNodes: 1,
                proofProbability: 5,
                duration: TimeSpan.FromMinutes(1));

            purchaseContract.WaitForStorageContractStarted(fileSize);

            AssertBalance(geth, contracts, seller, Is.LessThan(sellerInitialBalance), "Collateral was not placed.");

            purchaseContract.WaitForStorageContractFinished();

            AssertBalance(geth, contracts, seller, Is.GreaterThan(sellerInitialBalance), "Seller was not paid for storage.");
            AssertBalance(geth, contracts, buyer, Is.LessThan(buyerInitialBalance), "Buyer was not charged for storage.");
        }
    }
}
