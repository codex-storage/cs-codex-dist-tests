using DistTestCore.Logs;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private DateTime testStart = DateTime.MinValue;

        public TestLifecycle(BaseLog log, Configuration configuration, ITimeSet timeSet)
            : this(log, configuration, timeSet, new WorkflowCreator(log, configuration.GetK8sConfiguration(timeSet)))
        {
        }

        public TestLifecycle(BaseLog log, Configuration configuration, ITimeSet timeSet, WorkflowCreator workflowCreator)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;

            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(this, workflowCreator);
            PrometheusStarter = new PrometheusStarter(this, workflowCreator);
            GethStarter = new GethStarter(this, workflowCreator);
            testStart = DateTime.UtcNow;

            Log.WriteLogTag();
        }

        public BaseLog Log { get; }
        public Configuration Configuration { get; }
        public ITimeSet TimeSet { get; }
        public FileManager FileManager { get; }
        public CodexStarter CodexStarter { get; }
        public PrometheusStarter PrometheusStarter { get; }
        public GethStarter GethStarter { get; }

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
    }
}
