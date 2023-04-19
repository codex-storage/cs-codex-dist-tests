using DistTestCore.Logs;
using KubernetesWorkflow;
using Logging;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly WorkflowCreator workflowCreator;

        public TestLifecycle(TestLog log, Configuration configuration)
        {
            Log = log;
            workflowCreator = new WorkflowCreator(configuration.GetK8sConfiguration());

            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(this, workflowCreator);
            PrometheusStarter = new PrometheusStarter(this, workflowCreator);
            GethStarter = new GethStarter(this, workflowCreator);
        }

        public TestLog Log { get; }
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
            var description = node.Describe();
            var handler = new LogDownloadHandler(node, description, subFile);

            Log.Log($"Downloading logs for {description} to file '{subFile.FullFilename}'");
            CodexStarter.DownloadLog(node.CodexAccess.Container, handler);

            return new CodexNodeLog(subFile, node);
        }
    }
}
