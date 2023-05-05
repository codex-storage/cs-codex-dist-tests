using DistTestCore.Logs;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly WorkflowCreator workflowCreator;
        private DateTime testStart = DateTime.MinValue;

        public TestLifecycle(TestLog log, Configuration configuration, ITimeSet timeSet)
        {
            Log = log;
            TimeSet = timeSet;
            workflowCreator = new WorkflowCreator(log, configuration.GetK8sConfiguration(timeSet));

            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(this, workflowCreator);
            PrometheusStarter = new PrometheusStarter(this, workflowCreator);
            GethStarter = new GethStarter(this, workflowCreator);
            testStart = DateTime.UtcNow;
        }

        public TestLog Log { get; }
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

        public ICodexNodeLog DownloadLog(OnlineCodexNode node)
        {
            var subFile = Log.CreateSubfile();
            var description = node.GetName();
            var handler = new LogDownloadHandler(node, description, subFile);

            Log.Log($"Downloading logs for {description} to file '{subFile.FullFilename}'");
            CodexStarter.DownloadLog(node.CodexAccess.Container, handler);

            return new CodexNodeLog(subFile, node);
        }

        public string GetTestDuration()
        {
            var testDuration = DateTime.UtcNow - testStart;
            return Time.FormatDuration(testDuration);
        }
    }
}
