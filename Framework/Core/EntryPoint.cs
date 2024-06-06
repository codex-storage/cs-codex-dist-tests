using KubernetesWorkflow;
using Logging;

namespace Core
{
    public class EntryPoint
    {
        private readonly IToolsFactory toolsFactory;
        private readonly PluginManager manager = new PluginManager();

        public EntryPoint(ILog log, Configuration configuration, string fileManagerRootFolder, ITimeSet timeSet)
        {
            toolsFactory = new ToolsFactory(log, configuration, fileManagerRootFolder, timeSet);

            Tools = toolsFactory.CreateTools();
            manager.InstantiatePlugins(PluginFinder.GetPluginTypes(), toolsFactory);
        }

        public EntryPoint(ILog log, Configuration configuration, string fileManagerRootFolder)
            : this(log, configuration, fileManagerRootFolder, new DefaultTimeSet())
        {
        }

        public IPluginTools Tools { get; }

        public void Announce()
        {
            manager.AnnouncePlugins();
        }

        public Dictionary<string, string> GetPluginMetadata()
        {
            return manager.GatherPluginMetadata().Get();
        }

        public CoreInterface CreateInterface()
        {
            return new CoreInterface(this);
        }

        /// <summary>
        /// Deletes kubernetes and tracked file resources.
        /// when `waitTillDone` is true, this function will block until resources are deleted.
        /// </summary>
        public void Decommission(bool deleteKubernetesResources, bool deleteTrackedFiles, bool waitTillDone)
        {
            manager.DecommissionPlugins(deleteKubernetesResources, deleteTrackedFiles, waitTillDone);
            Tools.Decommission(deleteKubernetesResources, deleteTrackedFiles, waitTillDone);
        }

        internal T GetPlugin<T>() where T : IProjectPlugin
        {
            return manager.GetPlugin<T>();
        }
    }
}
