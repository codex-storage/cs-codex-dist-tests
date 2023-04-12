using Logging;

namespace DistTestCore
{
    public class TestLifecycle
    {
        public TestLifecycle(Configuration configuration)
        {
            Log = new TestLog(configuration.GetLogConfig());
            FileManager = new FileManager(Log, configuration);
            CodexStarter = new CodexStarter(Log, configuration);
        }

        public TestLog Log { get; }
        public FileManager FileManager { get; }
        public CodexStarter CodexStarter { get; }

        public void DeleteAllResources()
        {
            CodexStarter.DeleteAllResources();
            FileManager.DeleteAllTestFiles();
        }
    }
}
