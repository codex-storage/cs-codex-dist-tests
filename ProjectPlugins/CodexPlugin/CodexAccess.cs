using CodexOpenApi;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace CodexPlugin
{
    public class CodexAccess
    {
        private readonly ILog log;
        private readonly IPluginTools tools;
        private readonly Mapper mapper = new Mapper();

        public CodexAccess(IPluginTools tools, RunningPod container, CrashWatcher crashWatcher)
        {
            this.tools = tools;
            log = tools.GetLog();
            Container = container;
            CrashWatcher = crashWatcher;

            CrashWatcher.Start();
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
            return CrashCheck(() =>
            {
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
            });
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
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                return Time.Retry(() =>
                {
                    var str = endpoint.HttpGetString($"storage/purchases/{purchaseId}");
                    if (string.IsNullOrEmpty(str)) throw new Exception("Empty response.");
                    return JsonConvert.DeserializeObject<StoragePurchase>(str)!;
                }, nameof(GetPurchaseStatus));
            });

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

        public void LogDiskSpace(string msg)
        {
            what to do with this?
            //try
            //{
            //    var diskInfo = tools.CreateWorkflow().ExecuteCommand(Container.Containers.Single(), "df", "--sync");
            //    Log($"{msg} - Disk info: {diskInfo}");
            //}
            //catch (Exception e)
            //{
            //    Log("Failed to get disk info: " + e);
            //}
        }

        public void DeleteRepoFolder()
        {
            try
            {
                var containerNumber = Container.Containers.First().Recipe.Number;
                var dataDir = $"datadir{containerNumber}";
                var workflow = tools.CreateWorkflow();
                workflow.ExecuteCommand(Container.Containers.First(), "rm", "-Rfv", $"/codex/{dataDir}/repo");
                Log("Deleted repo folder.");
            }
            catch (Exception e)
            {
                Log("Unable to delete repo folder: " + e);
            }
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
            return CrashCheck(() => Time.Wait(action(api)));
        }

        private T CrashCheck<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            finally
            {
                CrashWatcher.HasContainerCrashed();
            }
        }

        private IEndpoint GetEndpoint()
        {
            return tools
                .CreateHttp(CheckContainerCrashed)
                .CreateEndpoint(GetAddress(), "/api/codex/v1/", Container.Name);
        }

        private Address GetAddress()
        {
            return Container.Containers.Single().GetAddress(log, CodexContainerRecipe.ApiPortTag);
        }

        private void CheckContainerCrashed(HttpClient client)
        {
            if (CrashWatcher.HasContainerCrashed()) throw new Exception($"Container {GetName()} has crashed.");
        }

        private Retry CreateRetryConfig(string description, Action<Failure> onFailure)
        {
            var timeSet = tools.TimeSet;

            return new Retry(description, timeSet.HttpRetryTimeout(), timeSet.HttpCallRetryDelay(), failure =>
            {
                onFailure(failure);
                Investigate(failure, timeSet);
            });
        }

        private void Investigate(Failure failure, ITimeSet timeSet)
        {
            Log($"Retry {failure.TryNumber} took {Time.FormatDuration(failure.Duration)} and failed with '{failure.Exception}'. " +
                $"(HTTP timeout = {Time.FormatDuration(timeSet.HttpCallTimeout())}) " +
                $"Checking if node responds to debug/info...");

            LogDiskSpace("After retry failure");

            try
            {
                var debugInfo = GetDebugInfo();
                if (string.IsNullOrEmpty(debugInfo.Spr))
                {
                    Log("Did not get value debug/info response.");
                    Throw(failure);
                }
                else
                {
                    Log("Got valid response from debug/info.");
                }
            }
            catch (Exception ex)
            {
                Log("Got exception from debug/info call: " + ex);
                Throw(failure);
            }

            if (failure.Duration < timeSet.HttpCallTimeout())
            {
                Log("Retry failed within HTTP timeout duration.");
                Throw(failure);
            }
        }

        private void Throw(Failure failure)
        {
            throw failure.Exception;
        }

        private void Log(string msg)
        {
            log.Log($"{GetName()} {msg}");
        }
    }
}
