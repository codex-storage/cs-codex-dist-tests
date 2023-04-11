using k8s.Models;

namespace CodexDistTestCore.Marketplace
{
    public class GethCompanionNodeContainer
    {
        public GethCompanionNodeContainer(string name, int apiPort, int rpcPort, string containerPortName)
        {
            Name = name;
            ApiPort = apiPort;
            RpcPort = rpcPort;
            ContainerPortName = containerPortName;
        }

        public string Name { get; }
        public int ApiPort { get; }
        public int RpcPort { get; }
        public string ContainerPortName { get; }
            
    }
}
