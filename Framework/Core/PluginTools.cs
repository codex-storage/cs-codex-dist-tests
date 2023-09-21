using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace Core
{
    public interface IPluginTools : IWorkflowTool, ILogTool, IHttpFactoryTool, IFileTool
    {
        void Decommission(bool deleteKubernetesResources, bool deleteTrackedFiles);
    }

    public interface IWorkflowTool
    {
        IStartupWorkflow CreateWorkflow(string? namespaceOverride = null);
    }

    public interface ILogTool
    {
        ILog GetLog();
    }

    public interface IHttpFactoryTool
    {
        IHttp CreateHttp(Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null);
        IHttp CreateHttp(Address address, string baseUrl, string? logAlias = null);
    }

    public interface IFileTool
    {
        IFileManager GetFileManager();
    }

    internal class PluginTools : IPluginTools
    {
        private readonly ITimeSet timeSet;
        private readonly WorkflowCreator workflowCreator;
        private readonly IFileManager fileManager;
        private ILog log;

        internal PluginTools(ILog log, WorkflowCreator workflowCreator, string fileManagerRootFolder, ITimeSet timeSet)
        {
            this.log = log;
            this.workflowCreator = workflowCreator;
            this.timeSet = timeSet;
            fileManager = new FileManager(log, fileManagerRootFolder);
        }

        public void ApplyLogPrefix(string prefix)
        {
            log = new LogPrefixer(log, prefix);
        }

        public IHttp CreateHttp(Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null)
        {
            return new Http(log, timeSet, address, baseUrl, onClientCreated, logAlias);
        }

        public IHttp CreateHttp(Address address, string baseUrl, string? logAlias = null)
        {
            return new Http(log, timeSet, address, baseUrl, logAlias);
        }

        public IStartupWorkflow CreateWorkflow(string? namespaceOverride = null)
        {
            return workflowCreator.CreateWorkflow(namespaceOverride);
        }

        public void Decommission(bool deleteKubernetesResources, bool deleteTrackedFiles)
        {
            if (deleteKubernetesResources) CreateWorkflow().DeleteNamespace();
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
