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

        public void Decommission()
        {
            manager.DecommissionPlugins();
        }

        internal T GetPlugin<T>() where T : IProjectPlugin
        {
            return manager.GetPlugin<T>();
        }
    }
}
