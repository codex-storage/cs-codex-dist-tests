using Core;
using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace DistTestCore
{
    public class TestLifecycle : IK8sHooks
    {
        private readonly DateTime testStart;
        private readonly EntryPoint entryPoint;
        private readonly List<RunningContainers> runningContainers = new List<RunningContainers>();

        public TestLifecycle(TestLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            Log = log;
            Configuration = configuration;
            TimeSet = timeSet;
            testStart = DateTime.UtcNow;

            entryPoint = new EntryPoint(log, configuration.GetK8sConfiguration(timeSet, this, testNamespace), configuration.GetFileManagerFolder(), timeSet);
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

        public void OnContainersStarted(RunningContainers rc)
        {
            runningContainers.Add(rc);
        }

        public void OnContainersStopped(RunningContainers rc)
        {
            runningContainers.Remove(rc);
        }

        public void DownloadAllLogs()
        {
            var workflow = entryPoint.Tools.CreateWorkflow();
            foreach (var rc in runningContainers)
            {
                foreach (var c in rc.Containers)
                {
                    DownloadContainerLog(workflow, c);
                }
            }
        }

        private void DownloadContainerLog(IStartupWorkflow workflow, RunningContainer c)
        {
            var file = Log.CreateSubfile();
            Log.Log($"Downloading container log for '{c.Name}' to file '{file.FullFilename}'...");
            var handler = new LogDownloadHandler(c.Name, file);
            workflow.DownloadContainerLog(c, handler);
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
