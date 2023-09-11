using DistTestCore.Codex;
using DistTestCore.Logs;
using DistTestCore.Marketplace;
using DistTestCore.Metrics;
using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly DateTime testStart;

        public TestLifecycle(BaseLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;

            WorkflowCreator = new WorkflowCreator(log, configuration.GetK8sConfiguration(timeSet), testNamespace);

            FileManager = new FileManager(Log, configuration.GetFileManagerFolder());
            CodexStarter = new CodexStarter(this);
            PrometheusStarter = new PrometheusStarter(this);
            GrafanaStarter = new GrafanaStarter(this);
            GethStarter = new GethStarter(this);
            testStart = DateTime.UtcNow;
            CodexVersion = null;

            Log.WriteLogTag();
        }

        public BaseLog Log { get; }
        public Configuration Configuration { get; }
        public ITimeSet TimeSet { get; }
        public WorkflowCreator WorkflowCreator { get; }
        public FileManager FileManager { get; }
        public CodexStarter CodexStarter { get; }
        public PrometheusStarter PrometheusStarter { get; }
        public GrafanaStarter GrafanaStarter { get; }
        public GethStarter GethStarter { get; }
        public CodexDebugVersionResponse? CodexVersion { get; private set; }

        public void DeleteAllResources()
        {
            CodexStarter.DeleteAllResources();
            FileManager.DeleteAllTestFiles();
        }

        public IDownloadedLog DownloadLog(RunningContainer container, int? tailLines = null)
        {
            var subFile = Log.CreateSubfile();
            var description = container.Name;
            var handler = new LogDownloadHandler(container, description, subFile);

            Log.Log($"Downloading logs for {description} to file '{subFile.FullFilename}'");
            CodexStarter.DownloadLog(container, handler, tailLines);

            return new DownloadedLog(subFile, description);
        }

        public string GetTestDuration()
        {
            var testDuration = DateTime.UtcNow - testStart;
            return Time.FormatDuration(testDuration);
        }

        public void SetCodexVersion(CodexDebugVersionResponse version)
        {
            if (CodexVersion == null) CodexVersion = version;
        }

        public ApplicationIds GetApplicationIds()
        {
            return new ApplicationIds(
                codexId: GetCodexId(),
                gethId: new GethContainerRecipe().Image,
                prometheusId: new PrometheusContainerRecipe().Image,
                codexContractsId: new CodexContractsContainerRecipe().Image,
                grafanaId: new GrafanaContainerRecipe().Image
            );
        }

        private string GetCodexId()
        {
            var v = CodexVersion;
            if (v == null) return new CodexContainerRecipe().Image;
            if (v.version != "untagged build") return v.version;
            return v.revision;
        }
    }
}
