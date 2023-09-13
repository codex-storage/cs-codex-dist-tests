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

        public IFileManager GetFileManager()
        {
            return entryPoint.Tools.GetFileManager();
        }

        public string GetTestDuration()
        {
            var testDuration = DateTime.UtcNow - testStart;
            return Time.FormatDuration(testDuration);
        }

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
