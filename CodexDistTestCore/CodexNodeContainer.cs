namespace CodexDistTestCore
{
    public class CodexNodeContainer
    {
        public CodexNodeContainer(string name, int servicePort, string servicePortName, int apiPort, string containerPortName, int discoveryPort, int listenPort, string dataDir)
        {
            Name = name;
            ServicePort = servicePort;
            ServicePortName = servicePortName;
            ApiPort = apiPort;
            ContainerPortName = containerPortName;
            DiscoveryPort = discoveryPort;
            ListenPort = listenPort;
            DataDir = dataDir;
        }

        public string Name { get; }
        public int ServicePort { get; }
        public string ServicePortName { get; }
        public int ApiPort { get; }
        public string ContainerPortName { get; }
        public int DiscoveryPort { get; }
        public int ListenPort { get; }
        public string DataDir { get; }
    }

    public class CodexNodeContainerFactory
    {
        private readonly NumberSource containerNameSource = new NumberSource(1);
        private readonly NumberSource servicePortSource = new NumberSource(30001);
        private readonly NumberSource codexPortSource = new NumberSource(8080);

        public CodexNodeContainer CreateNext()
        {
            var n = containerNameSource.GetNextNumber();
            return new CodexNodeContainer(
                name: $"codex-node{n}",
                servicePort: servicePortSource.GetNextNumber(),
                servicePortName: $"node{n}",
                apiPort: codexPortSource.GetNextNumber(),
                containerPortName: $"api-{n}",
                discoveryPort: codexPortSource.GetNextNumber(),
                listenPort: codexPortSource.GetNextNumber(),
                dataDir: $"datadir{n}"
            );
        }
    }
}
