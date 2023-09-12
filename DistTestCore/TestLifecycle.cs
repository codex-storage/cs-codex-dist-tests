using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle : IPluginTools
    {
        private readonly PluginManager pluginManager;
        private readonly DateTime testStart;

        public TestLifecycle(TestLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;
            TestNamespace = testNamespace;
            testStart = DateTime.UtcNow;
            FileManager = new FileManager(Log, Configuration.GetFileManagerFolder());

            pluginManager = new PluginManager();
            pluginManager.DiscoverPlugins();
            pluginManager.InitializePlugins(this);

            log.WriteLogTag();
        }

        public TestLog Log { get; }
        public Configuration Configuration { get; }
        public ITimeSet TimeSet { get; }
        public string TestNamespace { get; }
        public IFileManager FileManager { get; }

        public Http CreateHttp(Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null)
        {
            return new Http(Log, TimeSet, address, baseUrl, onClientCreated, logAlias);
        }

        public Http CreateHttp(Address address, string baseUrl, string? logAlias = null)
        {
            return new Http(Log, TimeSet, address, baseUrl, logAlias);
        }

        public IStartupWorkflow CreateWorkflow(string? namespaceOverride = null)
        {
            if (namespaceOverride != null) throw new Exception("Namespace override is not supported in the DistTest environment. (It would mess up automatic resource cleanup.)");
            var wc = new WorkflowCreator(Log, Configuration.GetK8sConfiguration(TimeSet), TestNamespace);
            return wc.CreateWorkflow();
        }

        public IFileManager GetFileManager()
        {
            return FileManager;
        }

        public ILog GetLog()
        {
            return Log;
        }

        public void DeleteAllResources()
        {
            CreateWorkflow().DeleteNamespace();
            FileManager.DeleteAllTestFiles();
        }

        //public IDownloadedLog DownloadLog(RunningContainer container, int? tailLines = null)
        //{
        //    var subFile = Log.CreateSubfile();
        //    var description = container.Name;
        //    var handler = new LogDownloadHandler(container, description, subFile);

        //    Log.Log($"Downloading logs for {description} to file '{subFile.FullFilename}'");
        //    //CodexStarter.DownloadLog(container, handler, tailLines);

        //    return new DownloadedLog(subFile, description);
        //}

        public string GetTestDuration()
        {
            var testDuration = DateTime.UtcNow - testStart;
            return Time.FormatDuration(testDuration);
        }

        ////public void SetCodexVersion(CodexDebugVersionResponse version)
        ////{
        ////    if (CodexVersion == null) CodexVersion = version;
        ////}

        //public ApplicationIds GetApplicationIds()
        //{
        //    //return new ApplicationIds(
        //    //    codexId: GetCodexId(),
        //    //    gethId: new GethContainerRecipe().Image,
        //    //    prometheusId: new PrometheusContainerRecipe().Image,
        //    //    codexContractsId: new CodexContractsContainerRecipe().Image,
        //    //    grafanaId: new GrafanaContainerRecipe().Image
        //    //);
        //    return null!;
        //}

        //private string GetCodexId()
        //{
        //    return "";
        //    //var v = CodexVersion;
        //    //if (v == null) return new CodexContainerRecipe().Image;
        //    //if (v.version != "untagged build") return v.version;
        //    //return v.revision;
        //}
    }
}
