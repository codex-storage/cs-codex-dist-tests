using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace Core
{
    public class EntryPoint : IPluginTools
    {
        private readonly PluginManager manager = new PluginManager();
        private readonly ILog log;
        private readonly ITimeSet timeSet;
        private readonly FileManager fileManager;
        private readonly WorkflowCreator workflowCreator;

        public EntryPoint(ILog log, Configuration configuration, string fileManagerRootFolder, ITimeSet timeSet)
        {
            this.log = log;
            this.timeSet = timeSet;
            fileManager = new FileManager(log, fileManagerRootFolder);
            workflowCreator = new WorkflowCreator(log, configuration);

            manager.InstantiatePlugins(PluginFinder.GetPluginTypes());
        }

        public EntryPoint(ILog log, Configuration configuration, string fileManagerRootFolder)
            : this(log, configuration, fileManagerRootFolder, new DefaultTimeSet())
        {
        }

        public void Announce()
        {
            manager.AnnouncePlugins(log);
        }

        public void Initialize()
        {
            manager.InitializePlugins(this);
        }

        public CoreInterface CreateInterface()
        {
            return new CoreInterface(this);
        }

        public void Decommission()
        {
            manager.FinalizePlugins(log);
        }

        internal T GetPlugin<T>() where T : IProjectPlugin
        {
            return manager.GetPlugin<T>();
        }

        public Http CreateHttp(Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null)
        {
            return new Http(log, timeSet, address, baseUrl, onClientCreated, logAlias);
        }

        public Http CreateHttp(Address address, string baseUrl, string? logAlias = null)
        {
            return new Http(log, timeSet, address, baseUrl, logAlias);
        }

        public IStartupWorkflow CreateWorkflow(string? namespaceOverride = null)
        {
            if (namespaceOverride != null) throw new Exception("Namespace override is not supported in the DistTest environment. (It would mess up automatic resource cleanup.)");
            return workflowCreator.CreateWorkflow();
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
