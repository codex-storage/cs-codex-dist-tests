namespace CodexDistTestCore.Marketplace
{
    public class GethCompanionGroup
    {
        public GethCompanionGroup(int number, GethCompanionNodeContainer[] containers)
        {
            Number = number;
            Containers = containers;
        }

        public int Number { get; }
        public GethCompanionNodeContainer[] Containers { get; }
        public PodInfo? Pod { get; set; }
    }

    public class GethCompanionNodeContainer
    {
        public GethCompanionNodeContainer(string name, int apiPort, int rpcPort, string containerPortName, int authRpcPort)
        {
            Name = name;
            ApiPort = apiPort;
            AuthRpcPort = authRpcPort;
            RpcPort = rpcPort;
            ContainerPortName = containerPortName;
        }

        public string Name { get; }
        public int ApiPort { get; }
        public int AuthRpcPort { get; }
        public int RpcPort { get; }
        public string ContainerPortName { get; }

        public string Account { get; set; } = string.Empty;
    }
}
