using FileUtils;
using KubernetesWorkflow;
using Logging;

namespace Core
{
    public interface IPluginTools : IWorkflowTool, ILogTool, IHttpFactoryTool, IFileTool
    {
        ITimeSet TimeSet { get; }
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
        IHttp CreateHttp(Action<HttpClient> onClientCreated);
        IHttp CreateHttp(Action<HttpClient> onClientCreated, ITimeSet timeSet);
        IHttp CreateHttp();
    }

    public interface IFileTool
    {
        IFileManager GetFileManager();
    }

    internal class PluginTools : IPluginTools
    {
        private readonly WorkflowCreator workflowCreator;
        private readonly IFileManager fileManager;
        private readonly LogPrefixer log;

        internal PluginTools(ILog log, WorkflowCreator workflowCreator, string fileManagerRootFolder, ITimeSet timeSet)
        {
            this.log = new LogPrefixer(log);
            this.workflowCreator = workflowCreator;
            TimeSet = timeSet;
            fileManager = new FileManager(log, fileManagerRootFolder);
        }

        public ITimeSet TimeSet { get; }

        public void ApplyLogPrefix(string prefix)
        {
            log.Prefix = prefix;
        }

        public IHttp CreateHttp(Action<HttpClient> onClientCreated)
        {
            return CreateHttp(onClientCreated, TimeSet);
        }

        public IHttp CreateHttp(Action<HttpClient> onClientCreated, ITimeSet ts)
        {
            return new Http(log, ts, onClientCreated);
        }

        public IHttp CreateHttp()
        {
            return new Http(log, TimeSet);
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
