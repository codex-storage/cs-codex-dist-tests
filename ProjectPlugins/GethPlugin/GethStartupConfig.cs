namespace GethPlugin
{
    public interface IGethSetup
    {
        IGethSetup IsMiner();
        IGethSetup WithBootstrapNode(GethBootstrapNode node);
        IGethSetup WithName(string name);
        IGethSetup AsPublicTestNet();
    }

    public class GethStartupConfig : IGethSetup
    {
        public bool IsMiner { get; private set; }
        public GethBootstrapNode? BootstrapNode { get; private set; }
        public string? NameOverride { get; private set; }
        public bool IsPublicTestNet { get; private set; } = false;

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

        public IGethSetup AsPublicTestNet()
        {
            IsPublicTestNet = true;
            return this;
        }
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
