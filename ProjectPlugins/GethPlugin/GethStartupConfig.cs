namespace GethPlugin
{
    public interface IGethSetup
    {
        IGethSetup IsMiner();
        IGethSetup WithBootstrapNode(IGethNode node);
        IGethSetup WithBootstrapNode(GethBootstrapNode node);
        IGethSetup WithName(string name);
        IGethSetup AsPublicTestNet(GethTestNetConfig gethTestNetConfig);
    }

    public class GethStartupConfig : IGethSetup
    {
        public bool IsMiner { get; private set; }
        public GethBootstrapNode? BootstrapNode { get; private set; }
        public string? NameOverride { get; private set; }
        public GethTestNetConfig? IsPublicTestNet { get; private set; }

        public IGethSetup WithBootstrapNode(IGethNode node)
        {
            return WithBootstrapNode(node.GetBootstrapRecord());
        }

        public IGethSetup WithBootstrapNode(GethBootstrapNode node)
        {
            BootstrapNode = node;
            return this;
        }

        public IGethSetup WithName(string name)
        {
            NameOverride = name;
            return this;
        }

        IGethSetup IGethSetup.IsMiner()
        {
            IsMiner = true;
            return this;
        }

        public IGethSetup AsPublicTestNet(GethTestNetConfig gethTestNetConfig)
        {
            IsPublicTestNet = gethTestNetConfig;
            return this;
        }
    }

    public class GethTestNetConfig
    {
        public GethTestNetConfig(int discoveryPort, int listenPort)
        {
            DiscoveryPort = discoveryPort;
            ListenPort = listenPort;
        }

        public int DiscoveryPort { get; }
        public int ListenPort { get; }
    }

    public class GethBootstrapNode
    {
        public GethBootstrapNode(string publicKey, string ipAddress, int port)
        {
            PublicKey = publicKey;
            IpAddress = ipAddress;
            Port = port;
        }

        public string PublicKey { get; }
        public string IpAddress { get; }
        public int Port { get; }
    }
}
