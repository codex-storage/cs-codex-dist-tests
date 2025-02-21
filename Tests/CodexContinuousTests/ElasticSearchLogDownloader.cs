using Core;
using KubernetesWorkflow.Types;
using Logging;
using Utils;
using WebUtils;

namespace ContinuousTests
{
    public class ElasticSearchLogDownloader
    {
        private readonly IPluginTools tools;
        private readonly ILog log;

        public ElasticSearchLogDownloader(IPluginTools tools, ILog log)
        {
            this.tools = tools;
            this.log = log;
        }

        public void Download(LogFile targetFile, RunningContainer container, DateTime startUtc, DateTime endUtc, string openingLine)
        {
            try
            {
                DownloadLog(targetFile, container, startUtc, endUtc, openingLine);
            }
            catch (Exception ex)
            {
                log.Error("Failed to download log: " + ex);
            }
        }

        private void DownloadLog(LogFile targetFile, RunningContainer container, DateTime startUtc, DateTime endUtc, string openingLine)
        {
            log.Log($"Downloading log (from ElasticSearch) for container '{container.Name}' within time range: " +
                $"{startUtc.ToString("o")} - {endUtc.ToString("o")}");
            log.Log(openingLine);

            var endpoint = CreateElasticSearchEndpoint();
            var queryTemplate = CreateQueryTemplate(container, startUtc, endUtc);

            targetFile.Write($"Downloading '{container.Name}' to '{targetFile.Filename}'.");
            var reconstructor = new LogReconstructor(targetFile, endpoint, queryTemplate);
            reconstructor.DownloadFullLog();

            log.Log("Log download finished.");
        }

        private string CreateQueryTemplate(RunningContainer container, DateTime startUtc, DateTime endUtc)
        {
            var start = startUtc.ToString("o");
            var end = endUtc.ToString("o");

            var containerName = container.RunningPod.StartResult.Deployment.Name;
            var namespaceName = container.RunningPod.StartResult.Cluster.Configuration.KubernetesNamespace;

            //container_name : codex3-5 - deploymentName as stored in pod
            // pod_namespace : codex - continuous - nolimits - tests - 1

            //var source = "{ \"sort\": [ { \"@timestamp\": { \"order\": \"asc\" } } ], \"fields\": [ { \"field\": \"@timestamp\", \"format\": \"strict_date_optional_time\" }, { \"field\": \"pod_name\" }, { \"field\": \"message\" } ], \"size\": <SIZE>, <SEARCHAFTER> \"_source\": false, \"query\": { \"bool\": { \"must\": [], \"filter\": [ { \"range\": { \"@timestamp\": { \"format\": \"strict_date_optional_time\", \"gte\": \"<STARTTIME>\", \"lte\": \"<ENDTIME>\" } } }, { \"match_phrase\": { \"pod_name\": \"<PODNAME>\" } } ] } } }";
            var source = "{ \"sort\": [ { \"@timestamp\": { \"order\": \"asc\" } } ], \"fields\": [ { \"field\": \"@timestamp\", \"format\": \"strict_date_optional_time\" }, { \"field\": \"message\" } ], \"size\": <SIZE>, <SEARCHAFTER> \"_source\": false, \"query\": { \"bool\": { \"must\": [], \"filter\": [ { \"range\": { \"@timestamp\": { \"format\": \"strict_date_optional_time\", \"gte\": \"<STARTTIME>\", \"lte\": \"<ENDTIME>\" } } }, { \"match_phrase\": { \"container_name\": \"<CONTAINERNAME>\" } }, { \"match_phrase\": { \"pod_namespace\": \"<NAMESPACENAME>\" } } ] } } }";
            return source
                .Replace("<STARTTIME>", start)
                .Replace("<ENDTIME>", end)
                .Replace("<CONTAINERNAME>", containerName)
                .Replace("<NAMESPACENAME>", namespaceName);
        }

        private IEndpoint CreateElasticSearchEndpoint()
        {
            var serviceName = "elasticsearch";
            var k8sNamespace = "monitoring";
            var address = new Address("ElasticSearchEndpoint", $"http://{serviceName}.{k8sNamespace}.svc.cluster.local", 9200);
            var baseUrl = "";

            var http = tools.CreateHttp(address.ToString(), client =>
            {
                client.DefaultRequestHeaders.Add("kbn-xsrf", "reporting");
            });

            return http.CreateEndpoint(address, baseUrl);
        }

        public class LogReconstructor
        {
            private readonly List<LogQueueEntry> queue = new List<LogQueueEntry>();
            private readonly LogFile targetFile;
            private readonly IEndpoint endpoint;
            private readonly string queryTemplate;
            private const int sizeOfPage = 2000;
            private string searchAfter = "";
            private int lastHits = 1;
            private ulong? lastLogLine;

            public LogReconstructor(LogFile targetFile, IEndpoint endpoint, string queryTemplate)
            {
                this.targetFile = targetFile;
                this.endpoint = endpoint;
                this.queryTemplate = queryTemplate;
            }

            public void DownloadFullLog()
            {
                while (lastHits > 0)
                {
                    QueryElasticSearch();
                    ProcessQueue();
                }
            }

            private void QueryElasticSearch()
            {
                var query = queryTemplate
                                .Replace("<SIZE>", sizeOfPage.ToString())
                                .Replace("<SEARCHAFTER>", searchAfter);

                var response = endpoint.HttpPostString<SearchResponse>("_search", query);

                lastHits = response.hits.hits.Length;
                if (lastHits > 0)
                {
                    UpdateSearchAfter(response);
                    foreach (var hit in response.hits.hits)
                    {
                        AddHitToQueue(hit);
                    }
                }
            }

            private void AddHitToQueue(SearchHitEntry hit)
            {
                var message = hit.fields.message.Single();
                var number = ParseCountNumber(message);
                if (number != null)
                {
                    queue.Add(new LogQueueEntry(message, number.Value));
                }
            }

            private ulong? ParseCountNumber(string message)
            {
                if (string.IsNullOrEmpty(message)) return null;
                var tokens = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (!tokens.Any()) return null;
                var countToken = tokens.SingleOrDefault(t => t.StartsWith("count="));
                if (countToken == null) return null;
                var number = countToken.Substring(6);
                if (ulong.TryParse(number, out ulong value))
                {
                    return value;
                }
                return null;
            }

            private void UpdateSearchAfter(SearchResponse response)
            {
                var uniqueSearchNumbers = response.hits.hits.Select(h => h.sort.Single()).Distinct().ToList();
                uniqueSearchNumbers.Reverse();

                var searchNumber = GetSearchNumber(uniqueSearchNumbers);
                searchAfter = $"\"search_after\": [{searchNumber}],";
            }

            private long GetSearchNumber(List<long> uniqueSearchNumbers)
            {
                if (uniqueSearchNumbers.Count == 1) return uniqueSearchNumbers.First();
                return uniqueSearchNumbers.Skip(1).First();
            }

            private void ProcessQueue()
            {
                if (lastLogLine == null)
                {
                    lastLogLine = queue.Min(q => q.Number) - 1;
                }

                while (queue.Any())
                {
                    ulong wantedNumber = lastLogLine.Value + 1;

                    DeleteOldEntries(wantedNumber);

                    var currentEntry = queue.FirstOrDefault(e => e.Number == wantedNumber);

                    if (currentEntry != null)
                    {
                        WriteEntryToFile(currentEntry);
                        queue.Remove(currentEntry);
                        lastLogLine = currentEntry.Number;
                    }
                    else
                    {
                        // The line number we want is not in the queue.
                        // It will be returned by the elastic search query, some time in the future.
                        // Stop processing the queue for now.
                        return;
                    }
                }
            }

            private void WriteEntryToFile(LogQueueEntry currentEntry)
            {
                targetFile.WriteRaw(currentEntry.Message);
            }

            private void DeleteOldEntries(ulong wantedNumber)
            {
                queue.RemoveAll(e => e.Number < wantedNumber);
            }

            public class LogQueueEntry
            {
                public LogQueueEntry(string message, ulong number)
                {
                    Message = message;
                    Number = number;
                }

                public string Message { get; }
                public ulong Number { get; }
            }

            public class SearchResponse
            {
                public SearchHits hits { get; set; } = new SearchHits();
            }

            public class SearchHits
            {
                public SearchHitEntry[] hits { get; set; } = Array.Empty<SearchHitEntry>();
            }

            public class SearchHitEntry
            {
                public SearchHitFields fields { get; set; } = new SearchHitFields();
                public long[] sort { get; set; } = Array.Empty<long>();
            }

            public class SearchHitFields
            {
                public string[] @timestamp { get; set; } = Array.Empty<string>();
                public string[] message { get; set; } = Array.Empty<string>();
            }
        }
    }
}
