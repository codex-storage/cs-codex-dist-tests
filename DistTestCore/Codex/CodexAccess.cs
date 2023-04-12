using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexAccess
    {
        private readonly RunningContainer runningContainer;

        public CodexAccess(RunningContainer runningContainer)
        {
            this.runningContainer = runningContainer;
        }

        public CodexDebugResponse GetDebugInfo()
        {
            var response = Http().HttpGetJson<CodexDebugResponse>("debug/info");
            //Log($"Got DebugInfo with id: '{response.id}'.");
            return response;
        }

        private Http Http()
        {
            var ip = runningContainer.Pod.Cluster.GetIp();
            var port = runningContainer.ServicePorts[0].Number;
            return new Http(ip, port, baseUrl: "/api/codex/v1");
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
