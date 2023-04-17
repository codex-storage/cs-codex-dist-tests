using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexAccess
    {
        public CodexAccess(RunningContainer runningContainer)
        {
            Container = runningContainer;
        }

        public RunningContainer Container { get; }

        public CodexDebugResponse GetDebugInfo()
        {
            return Http().HttpGetJson<CodexDebugResponse>("debug/info");
        }

        public string UploadFile(FileStream fileStream)
        {
            return Http().HttpPostStream("upload", fileStream);
        }

        public Stream DownloadFile(string contentId)
        {
            return Http().HttpGetStream("download/" + contentId);
        }

        private Http Http()
        {
            var ip = Container.Pod.Cluster.IP;
            var port = Container.ServicePorts[0].Number;
            return new Http(ip, port, baseUrl: "/api/codex/v1");
        }

        public string ConnectToPeer(string peerId, string peerMultiAddress)
        {
            return Http().HttpGetString($"connect/{peerId}?addrs={peerMultiAddress}");
        }
    }

    public class CodexDebugResponse
    {
        public string id { get; set; } = string.Empty;
        public string[] addrs { get; set; } = new string[0];
        public string repo { get; set; } = string.Empty;
        public string spr { get; set; } = string.Empty;
        public CodexDebugVersionResponse codex { get; set; } = new();
    }

    public class CodexDebugVersionResponse
    {
        public string version { get; set; } = string.Empty;
        public string revision { get; set; } = string.Empty;
    }
}
