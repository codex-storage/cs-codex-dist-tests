using Core;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly DateTime testStart;

        public TestLifecycle(TestLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;
            testStart = DateTime.UtcNow;

            EntryPoint = new EntryPoint(log, configuration.GetK8sConfiguration(timeSet, testNamespace), configuration.GetFileManagerFolder(), timeSet);
            EntryPoint.Initialize();

            log.WriteLogTag();
        }

        public TestLog Log { get; }
        public Configuration Configuration { get; }
        public ITimeSet TimeSet { get; }
        public EntryPoint EntryPoint { get; }

        public void DeleteAllResources()
        {
            EntryPoint.CreateWorkflow().DeleteNamespace();
            EntryPoint.GetFileManager().DeleteAllTestFiles();
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
