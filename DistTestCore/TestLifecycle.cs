using Core;
using FileUtils;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle
    {
        private readonly DateTime testStart;
        private readonly EntryPoint entryPoint;

        public TestLifecycle(TestLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;
            testStart = DateTime.UtcNow;

            entryPoint = new EntryPoint(log, configuration.GetK8sConfiguration(timeSet, testNamespace), configuration.GetFileManagerFolder(), timeSet);
            CoreInterface = entryPoint.CreateInterface();

            log.WriteLogTag();
        }

        public TestLog Log { get; }
        public Configuration Configuration { get; }
        public ITimeSet TimeSet { get; }
        public CoreInterface CoreInterface { get; }

        public void DeleteAllResources()
        {
            entryPoint.Tools.CreateWorkflow().DeleteNamespace();
            entryPoint.Tools.GetFileManager().DeleteAllFiles();
            entryPoint.Decommission();
        }

        public TrackedFile GenerateTestFile(ByteSize size, string label = "")
        {
            return entryPoint.Tools.GetFileManager().GenerateFile(size, label);
        }

        public void ScopedTestFiles(Action action)
        {
            entryPoint.Tools.GetFileManager().ScopedFiles(action);
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
