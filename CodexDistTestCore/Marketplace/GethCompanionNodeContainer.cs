using k8s.Models;

namespace CodexDistTestCore.Marketplace
{
    public class GethCompanionNodeContainer
    {
        public GethCompanionNodeContainer(string name, int servicePort, string servicePortName, int apiPort, int rpcPort, string containerPortName)
        {
            Name = name;
            ServicePort = servicePort;
            ServicePortName = servicePortName;
            ApiPort = apiPort;
            RpcPort = rpcPort;
            ContainerPortName = containerPortName;
        }

        public string Name { get; }
        public int ServicePort { get; }
        public string ServicePortName { get; }
        public int ApiPort { get; }
        public int RpcPort { get; }
        public string ContainerPortName { get; }

        public V1Container CreateDeploymentContainer(GethInfo gethInfo)
        {
            return new V1Container
            {
                Name = Name,
                Image = GethDockerImage.Image,
                Ports = new List<V1ContainerPort>
                    {
                        new V1ContainerPort
                        {
                            ContainerPort = ApiPort,
                            Name = ContainerPortName
                        }
                    },
                // todo: use env vars to connect this node to the bootstrap node provided by gethInfo.podInfo & gethInfo.servicePort & gethInfo.genesisJsonBase64
                Env = new List<V1EnvVar>
                {
                    new V1EnvVar
                    {
                        Name = "GETH_ARGS",
                        Value = $"--port {ApiPort} --discovery.port {ApiPort} --authrpc.port {RpcPort}"
                    },
                    new V1EnvVar
                    {
                        Name = "GENESIS_JSON",
                        Value = gethInfo.GenesisJsonBase64
                    }
                }
            };
        }

        public V1ServicePort CreateServicePort()
        {
            return new V1ServicePort
            {
                Name = ServicePortName,
                Protocol = "TCP",
                Port = ApiPort,
                TargetPort = ContainerPortName,
                NodePort = ServicePort
            };
        }
    }
}
