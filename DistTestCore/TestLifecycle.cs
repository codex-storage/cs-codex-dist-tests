using DistTestCore.Codex;
using DistTestCore.Logs;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly DateTime testStart;

        public TestLifecycle(BaseLog log, Configuration configuration, ITimeSet timeSet)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;

            WorkflowCreator = new WorkflowCreator(log, configuration.GetK8sConfiguration(timeSet), "dist-tests");

            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(this);
            PrometheusStarter = new PrometheusStarter(this);
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
        public GethStarter GethStarter { get; }
        public CodexDebugVersionResponse? CodexVersion { get; private set; }

        public void DeleteAllResources()
        {
            CodexStarter.DeleteAllResources();
            FileManager.DeleteAllTestFiles();
        }

        public IDownloadedLog DownloadLog(RunningContainer container)
        {
            var subFile = Log.CreateSubfile();
            var description = container.Name;
            var handler = new LogDownloadHandler(container, description, subFile);

            Log.Log($"Downloading logs for {description} to file '{subFile.FullFilename}'");
            CodexStarter.DownloadLog(container, handler);

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
    }
}
