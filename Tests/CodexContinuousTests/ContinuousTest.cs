using CodexContractsPlugin;
using CodexPlugin;
using Core;
using FileUtils;
using GethPlugin;
using Logging;
using MetricsPlugin;

namespace ContinuousTests
{
    public abstract class ContinuousTestLongTimeouts : ContinuousTest
    {
        public override ITimeSet TimeSet => new LongTimeSet();
    }

    public abstract class ContinuousTest
    {
        protected const int Zero = 0;
        protected const int MinuteOne = 60;
        protected const int MinuteFive = MinuteOne * 5;
        protected const int HourOne = MinuteOne * 60;
        protected const int HourThree = HourOne * 3;
        protected const int DayOne = HourOne * 24;
        protected const int DayThree = DayOne * 3;

        public void Initialize(ICodexNode[] nodes, ILog log, IFileManager fileManager, Configuration configuration, CancellationToken cancelToken)
        {
            Nodes = nodes;
            Log = log;
            FileManager = fileManager;
            Configuration = configuration;
            CancelToken = cancelToken;

            if (nodes != null)
            {
                NodeRunner = new NodeRunner(Nodes, configuration, Log);
            }
            else
            {
                NodeRunner = null!;
            }
        }

        public ICodexNode[] Nodes { get; private set; } = null!;
        public ILog Log { get; private set; } = null!;
        public IFileManager FileManager { get; private set; } = null!;
        public Configuration Configuration { get; private set; } = null!;
        public virtual ITimeSet TimeSet { get { return new DefaultTimeSet(); } }
        public CancellationToken CancelToken { get; private set; } = new CancellationToken();
        public NodeRunner NodeRunner { get; private set; } = null!;

        public IMetricsAccess CreateMetricsAccess(IHasMetricsScrapeTarget target)
        {
            return CreateMetricsAccess(target.MetricsScrapeTarget);
        }

        public IMetricsAccess CreateMetricsAccess(IMetricsScrapeTarget target)
        {
            if (Configuration.CodexDeployment.PrometheusContainer == null) throw new Exception("Expected prometheus to be part of Codex deployment.");

            var ci = CreateCi();
            return ci.WrapMetricsCollector(Configuration.CodexDeployment.PrometheusContainer, target);
        }

        public (IGethNode, ICodexContracts) CreateMarketplaceAccess()
        {
            var ci = CreateCi();

            var geth = ci.WrapGethDeployment(Configuration.CodexDeployment.GethDeployment);
            var contracts = ci.WrapCodexContractsDeployment(geth, Configuration.CodexDeployment.CodexContractsDeployment);

            return (geth, contracts);
        }

        public abstract int RequiredNumberOfNodes { get; }
        public abstract TimeSpan RunTestEvery { get; }
        public abstract TestFailMode TestFailMode { get; }

        public string Name
        {
            get
            {
                return GetType().Name;
            }
        }

        private CoreInterface CreateCi()
        {
            var entryPointFactory = new EntryPointFactory();
            var entryPoint = entryPointFactory.CreateEntryPoint(Configuration.KubeConfigFile, Configuration.DataPath, Configuration.CodexDeployment.Metadata.KubeNamespace, Log);
            return entryPoint.CreateInterface();
        }
    }

    public enum TestFailMode
    {
        StopAfterFirstFailure,
        AlwaysRunAllMoments
    }
}
