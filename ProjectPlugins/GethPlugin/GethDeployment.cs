using Core;
using KubernetesWorkflow.Recipe;
using KubernetesWorkflow.Types;
using Newtonsoft.Json;

namespace GethPlugin
{
    public class GethDeployment : IHasContainer
    {
        public GethDeployment(RunningContainers containers, Port discoveryPort, Port httpPort, Port wsPort, GethAccount account, string pubKey)
        {
            Containers = containers;
            DiscoveryPort = discoveryPort;
            HttpPort = httpPort;
            WsPort = wsPort;
            Account = account;
            PubKey = pubKey;
        }

        public RunningContainers Containers { get; }
        [JsonIgnore]
        public RunningContainer Container {  get { return Containers.Containers.Single(); } }
        public Port DiscoveryPort { get; }
        public Port HttpPort { get; }
        public Port WsPort { get; }
        public GethAccount Account { get; }
        public string PubKey { get; }
    }
}
