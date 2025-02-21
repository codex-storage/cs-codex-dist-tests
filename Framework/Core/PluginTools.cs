using FileUtils;
using KubernetesWorkflow;
using Logging;
using WebUtils;

namespace Core
{
    public interface IPluginTools : IWorkflowTool, ILogTool, IHttpFactory, IFileTool
    {
        IWebCallTimeSet WebCallTimeSet { get; }
        IK8sTimeSet K8STimeSet { get; }

        /// <summary>
        /// Deletes kubernetes and tracked file resources.
        /// when `waitTillDone` is true, this function will block until resources are deleted.
        /// </summary>
        void Decommission(bool deleteKubernetesResources, bool deleteTrackedFiles, bool waitTillDone);
    }

    public interface IWorkflowTool
    {
        IStartupWorkflow CreateWorkflow(string? namespaceOverride = null);
    }

    public interface ILogTool
    {
        ILog GetLog();
    }

    public interface IFileTool
    {
        IFileManager GetFileManager();
    }

    internal class PluginTools : IPluginTools
    {
        private readonly WorkflowCreator workflowCreator;
        private readonly HttpFactory httpFactory;
        private readonly IFileManager fileManager;
        private readonly LogPrefixer log;

        internal PluginTools(ILog log, WorkflowCreator workflowCreator, string fileManagerRootFolder, IWebCallTimeSet webCallTimeSet, IK8sTimeSet k8STimeSet)
        {
            this.log = new LogPrefixer(log);
            this.workflowCreator = workflowCreator;
            httpFactory = new HttpFactory(log, webCallTimeSet);
            WebCallTimeSet = webCallTimeSet;
            K8STimeSet = k8STimeSet;
            fileManager = new FileManager(log, fileManagerRootFolder);
        }

        public IWebCallTimeSet WebCallTimeSet { get; }
        public IK8sTimeSet K8STimeSet { get; }

        public void ApplyLogPrefix(string prefix)
        {
            log.Prefix = prefix;
        }

        public IHttp CreateHttp(string id, Action<HttpClient> onClientCreated)
        {
            return httpFactory.CreateHttp(id, onClientCreated);
        }

        public IHttp CreateHttp(string id, Action<HttpClient> onClientCreated, IWebCallTimeSet timeSet)
        {
            return httpFactory.CreateHttp(id, onClientCreated, timeSet);
        }

        public IHttp CreateHttp(string id)
        {
            return httpFactory.CreateHttp(id);
        }

        public IStartupWorkflow CreateWorkflow(string? namespaceOverride = null)
        {
            return workflowCreator.CreateWorkflow(namespaceOverride);
        }

        public void Decommission(bool deleteKubernetesResources, bool deleteTrackedFiles, bool waitTillDone)
        {
            if (deleteKubernetesResources) CreateWorkflow().DeleteNamespace(waitTillDone);
            if (deleteTrackedFiles) fileManager.DeleteAllFiles();
        }

        public IFileManager GetFileManager()
        {
            return fileManager;
        }

        public ILog GetLog()
        {
            return log;
        }
    }
}
