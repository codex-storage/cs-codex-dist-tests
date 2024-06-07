using CodexOpenApi;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public class CodexAccess : ILogHandler
    {
        private readonly IPluginTools tools;
        private readonly Mapper mapper = new Mapper();
        private bool hasContainerCrashed;

        public CodexAccess(IPluginTools tools, RunningPod container, CrashWatcher crashWatcher)
        {
            this.tools = tools;
            Container = container;
            CrashWatcher = crashWatcher;
            hasContainerCrashed = false;

            CrashWatcher.Start(this);
        }

        public RunningPod Container { get; }
        public CrashWatcher CrashWatcher { get; }

        public DebugInfo GetDebugInfo()
        {
            return mapper.Map(OnCodex(api => api.GetDebugInfoAsync()));
        }

        public DebugPeer GetDebugPeer(string peerId)
        {
            // Cannot use openAPI: debug/peer endpoint is not specified there.
            var endpoint = GetEndpoint();
            var str = endpoint.HttpGetString($"debug/peer/{peerId}");

            if (str.ToLowerInvariant() == "unable to find peer!")
            {
                return new DebugPeer
                {
                    IsPeerFound = false
                };
            }

            var result = endpoint.Deserialize<DebugPeer>(str);
            result.IsPeerFound = true;
            return result;
        }

        public void ConnectToPeer(string peerId, string[] peerMultiAddresses)
        {
            OnCodex(api =>
            {
                Time.Wait(api.ConnectPeerAsync(peerId, peerMultiAddresses));
                return Task.FromResult(string.Empty);
            });
        }

        public string UploadFile(FileStream fileStream, Action<Failure> onFailure)
        {
            return OnCodex(
                api => api.UploadAsync(fileStream),
                CreateRetryConfig(nameof(UploadFile), onFailure));
        }

        public Stream DownloadFile(string contentId, Action<Failure> onFailure)
        {
            var fileResponse = OnCodex(
                api => api.DownloadNetworkAsync(contentId),
                CreateRetryConfig(nameof(DownloadFile), onFailure));

            if (fileResponse.StatusCode != 200) throw new Exception("Download failed with StatusCode: " + fileResponse.StatusCode);
            return fileResponse.Stream;
        }

        public LocalDatasetList LocalFiles()
        {
            return mapper.Map(OnCodex(api => api.ListDataAsync()));
        }

        public StorageAvailability SalesAvailability(StorageAvailability request)
        {
            var body = mapper.Map(request);
            var read = OnCodex<SalesAvailabilityREAD>(api => api.OfferStorageAsync(body));
            return mapper.Map(read);
        }

        public string RequestStorage(StoragePurchaseRequest request)
        {
            var body = mapper.Map(request);
            return OnCodex<string>(api => api.CreateStorageRequestAsync(request.ContentId.Id, body));
        }

        public CodexSpace Space()
        {
            var space = OnCodex<Space>(api => api.SpaceAsync());
            return mapper.Map(space);
        }

        public StoragePurchase GetPurchaseStatus(string purchaseId)
        {
            var endpoint = GetEndpoint();
            return Time.Retry(() =>
            {
                var str = endpoint.HttpGetString($"storage/purchases/{purchaseId}");
                if (string.IsNullOrEmpty(str)) throw new Exception("Empty response.");
                return JsonConvert.DeserializeObject<StoragePurchase>(str)!;
            }, nameof(GetPurchaseStatus));

            // TODO: current getpurchase api does not line up with its openapi spec.
            // return mapper.Map(OnCodex(api => api.GetPurchaseAsync(purchaseId)));
        }

        public string GetName()
        {
            return Container.Name;
        }

        public PodInfo GetPodInfo()
        {
            var workflow = tools.CreateWorkflow();
            return workflow.GetPodInfo(Container);
        }

        private T OnCodex<T>(Func<CodexApi, Task<T>> action)
        {
            var result = tools.CreateHttp(CheckContainerCrashed).OnClient(client => CallCodex(client, action));
            return result;
        }

        private T OnCodex<T>(Func<CodexApi, Task<T>> action, Retry retry)
        {
            var result = tools.CreateHttp(CheckContainerCrashed).OnClient(client => CallCodex(client, action), retry);
            return result;
        }

        private T CallCodex<T>(HttpClient client, Func<CodexApi, Task<T>> action)
        {
            var address = GetAddress();
            var api = new CodexApi(client);
            api.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";
            return Time.Wait(action(api));
        }

        private IEndpoint GetEndpoint()
        {
            return tools
                .CreateHttp(CheckContainerCrashed)
                .CreateEndpoint(GetAddress(), "/api/codex/v1/", Container.Name);
        }

        private Address GetAddress()
        {
            return Container.Containers.Single().GetAddress(tools.GetLog(), CodexContainerRecipe.ApiPortTag);
        }

        private void CheckContainerCrashed(HttpClient client)
        {
            if (hasContainerCrashed) throw new Exception($"Container {GetName()} has crashed.");
        }

        void ILogHandler.Log(Stream crashLog)
        {
            var log = tools.GetLog();
            var file = log.CreateSubfile();
            log.Log($"Container {Container.Name} has crashed. Downloading crash log to '{file.FullFilename}'...");
            file.Write($"Container Crash Log for {Container.Name}.");

            using var reader = new StreamReader(crashLog);
            var line = reader.ReadLine();
            while (line != null)
            {
                file.Write(line);
                line = reader.ReadLine();
            }

            log.Log("Crash log successfully downloaded.");
            hasContainerCrashed = true;
        }

        private Retry CreateRetryConfig(string description, Action<Failure> onFailure)
        {
            var timeSet = tools.TimeSet;
            var log = tools.GetLog();

            return new Retry(description, timeSet.HttpRetryTimeout(), timeSet.HttpCallRetryDelay(), failure =>
            {
                onFailure(failure);
                Investigate(log, failure, timeSet);
            });
        }

        private void Investigate(ILog log, Failure failure, ITimeSet timeSet)
        {
            log.Log($"Retry {failure.TryNumber} took {Time.FormatDuration(failure.Duration)} and failed with '{failure.Exception}'. " +
                $"(HTTP timeout = {Time.FormatDuration(timeSet.HttpCallTimeout())}) " +
                $"Checking if node responds to debug/info...");

            try
            {
                var debugInfo = GetDebugInfo();
                if (string.IsNullOrEmpty(debugInfo.Spr))
                {
                    log.Log("Did not get value debug/info response.");
                    DownloadLog();
                    Throw(failure);
                }
                else
                {
                    log.Log("Got valid response from debug/info.");
                }
            }
            catch (Exception ex)
            {
                log.Log("Got exception from debug/info call: " + ex);
                DownloadLog();
                Throw(failure);
            }

            if (failure.Duration < timeSet.HttpCallTimeout())
            {
                log.Log("Retry failed within HTTP timeout duration.");
                DownloadLog();
                Throw(failure);
            }
        }

        private void Throw(Failure failure)
        {
            throw failure.Exception;
        }

        private void DownloadLog()
        {
            tools.CreateWorkflow().DownloadContainerLog(Container.Containers.Single(), this);
        }
    }
}
