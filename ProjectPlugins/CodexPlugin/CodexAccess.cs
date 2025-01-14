﻿using CodexOpenApi;
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

        public string GetSpr()
        {
            return CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                var json = endpoint.HttpGetString("spr");
                var response = JsonConvert.DeserializeObject<SprResponse>(json);
                return response!.Spr;
            });
        }

        private class SprResponse
        {
            public string Spr { get; set; } = string.Empty;
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

        public string UploadFile(UploadInput uploadInput, Action<Failure> onFailure)
        {
            return OnCodex(
                api => api.UploadAsync(uploadInput.ContentType, uploadInput.ContentDisposition, uploadInput.FileStream),
                CreateRetryConfig(nameof(UploadFile), onFailure));
        }

        public Stream DownloadFile(string contentId, Action<Failure> onFailure)
        {
            var fileResponse = OnCodex(
                api => api.DownloadNetworkStreamAsync(contentId),
                CreateRetryConfig(nameof(DownloadFile), onFailure));

            if (fileResponse.StatusCode != 200) throw new Exception("Download failed with StatusCode: " + fileResponse.StatusCode);
            return fileResponse.Stream;
        }

        public LocalDataset DownloadStreamless(ContentId cid)
        {
            var response = OnCodex(api => api.DownloadNetworkAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDataset DownloadManifestOnly(ContentId cid)
        {
            var response = OnCodex(api => api.DownloadNetworkManifestAsync(cid.Id));
            return mapper.Map(response);
        }

        public LocalDatasetList LocalFiles()
        {
            // API for listData mismatches.
            //return mapper.Map(OnCodex(api => api.ListDataAsync()));

            return mapper.Map(CrashCheck(() =>
            {
                var endpoint = GetEndpoint();
                return Time.Retry(() =>
                {
                    var str = endpoint.HttpGetString("data");
                    if (string.IsNullOrEmpty(str)) throw new Exception("Empty response.");
                    return JsonConvert.DeserializeObject<LocalDatasetListJson>(str)!;
                }, nameof(LocalFiles));
            }));

        }

        public StorageAvailability SalesAvailability(StorageAvailability request)
        {
            var body = mapper.Map(request);
            var read = OnCodex(api => api.OfferStorageAsync(body));
            return mapper.Map(read);
        }

        public StorageAvailability[] GetAvailabilities()
        {
            var collection = OnCodex(api => api.GetAvailabilitiesAsync());
            return mapper.Map(collection);
        }

        public string RequestStorage(StoragePurchaseRequest request)
        {
            var body = mapper.Map(request);
            return OnCodex(api => api.CreateStorageRequestAsync(request.ContentId.Id, body));
        }

        public CodexSpace Space()
        {
            var space = OnCodex(api => api.SpaceAsync());
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
            var result = tools.CreateHttp(GetHttpId(), CheckContainerCrashed).OnClient(client => CallCodex(client, action));
            return result;
        }

        private T OnCodex<T>(Func<CodexApi, Task<T>> action, Retry retry)
        {
            var result = tools.CreateHttp(GetHttpId(), CheckContainerCrashed).OnClient(client => CallCodex(client, action), retry);
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
                .CreateHttp(GetHttpId(), CheckContainerCrashed)
                .CreateEndpoint(GetAddress(), "/api/codex/v1/", Container.Name);
        }

        private Address GetAddress()
        {
            return Container.Containers.Single().GetAddress(CodexContainerRecipe.ApiPortTag);
        }

        private string GetHttpId()
        {
            return GetAddress().ToString();
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

    public class UploadInput
    {
        public UploadInput(string contentType, string contentDisposition, FileStream fileStream)
        {
            ContentType = contentType;
            ContentDisposition = contentDisposition;
            FileStream = fileStream;
        }

        public string ContentType { get; }
        public string ContentDisposition { get; }
        public FileStream FileStream { get; }
    }
}
