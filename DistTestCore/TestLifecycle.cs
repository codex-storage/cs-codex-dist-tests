using DistTestCore.CodexLogsAndMetrics;
using Logging;

namespace DistTestCore
{
    public class TestLifecycle
    {
        public TestLifecycle(Configuration configuration)
        {
            Log = new TestLog(configuration.GetLogConfig());
            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(this, configuration);
        }

        public TestLog Log { get; }
        public FileManager FileManager { get; }
        public CodexStarter CodexStarter { get; }

        public void DeleteAllResources()
        {
            CodexStarter.DeleteAllResources();
            FileManager.DeleteAllTestFiles();
        }

        public ICodexNodeLog DownloadLog(OnlineCodexNode node)
        {
            var subFile = Log.CreateSubfile();
            var description = node.Describe();
            var handler = new LogDownloadHandler(description, subFile);

            Log.Log($"Downloading logs for {description} to file {subFile.FilenameWithoutPath}");
            CodexStarter.DownloadLog(node.CodexAccess.Container, handler);

            return new CodexNodeLog(subFile);
        }
    }
}
