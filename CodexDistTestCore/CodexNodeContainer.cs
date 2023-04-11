namespace CodexDistTestCore
{
    public class CodexNodeContainer
    {
        public CodexNodeContainer(string name, int servicePort, string servicePortName, int apiPort, string containerPortName, int discoveryPort, int listenPort, string dataDir, int metricsPort)
        {
            Name = name;
            ServicePort = servicePort;
            ServicePortName = servicePortName;
            ApiPort = apiPort;
            ContainerPortName = containerPortName;
            DiscoveryPort = discoveryPort;
            ListenPort = listenPort;
            DataDir = dataDir;
            MetricsPort = metricsPort;
        }

        public string Name { get; }
        public int ServicePort { get; }
        public string ServicePortName { get; }
        public int ApiPort { get; }
        public string ContainerPortName { get; }
        public int DiscoveryPort { get; }
        public int ListenPort { get; }
        public string DataDir { get; }
        public int MetricsPort { get; }
    }

    public class CodexGroupNumberSource
    {
        private readonly NumberSource codexNodeGroupNumberSource = new NumberSource(0);
        private readonly NumberSource groupContainerNameSource = new NumberSource(1);
        private readonly NumberSource servicePortSource = new NumberSource(30001);

        public int GetNextCodexNodeGroupNumber()
        {
            return codexNodeGroupNumberSource.GetNextNumber();
        }

        public string GetNextServicePortName()
        {
            return $"node{groupContainerNameSource.GetNextNumber()}";
        }

        public int GetNextServicePort()
        {
            return servicePortSource.GetNextNumber();
        }
    }

    public class CodexNodeContainerFactory
    {
        private readonly NumberSource containerNameSource = new NumberSource(1);
        private readonly NumberSource codexPortSource = new NumberSource(8080);
        private readonly CodexGroupNumberSource numberSource;

        public CodexNodeContainerFactory(CodexGroupNumberSource numberSource)
        {
            this.numberSource = numberSource;
        }

        public CodexNodeContainer CreateNext(OfflineCodexNodes offline)
        {
            var n = containerNameSource.GetNextNumber();
            return new CodexNodeContainer(
                name: $"codex-node{n}",
                servicePort: numberSource.GetNextServicePort(),
                servicePortName: numberSource.GetNextServicePortName(),
                apiPort: codexPortSource.GetNextNumber(),
                containerPortName: $"api-{n}",
                discoveryPort: codexPortSource.GetNextNumber(),
                listenPort: codexPortSource.GetNextNumber(),
                dataDir: $"datadir{n}",
                metricsPort: GetMetricsPort(offline)
            );
        }

        private int GetMetricsPort(OfflineCodexNodes offline)
        {
            if (offline.MetricsEnabled) return codexPortSource.GetNextNumber();
            return 0;
        }

    }
}
