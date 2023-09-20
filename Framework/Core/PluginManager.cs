namespace Core
{
    internal class PluginManager
    {
        private readonly List<IProjectPlugin> projectPlugins = new List<IProjectPlugin>();

        internal void InstantiatePlugins(Type[] pluginTypes, IToolsFactory provider)
        {
            projectPlugins.Clear();
            foreach (var pluginType in pluginTypes)
            {
                var tools = provider.CreateTools();
                var plugin = InstantiatePlugins(pluginType, tools);

                ApplyLogPrefix(plugin, tools);
            }
        }

        internal void AnnouncePlugins()
        {
            foreach (var plugin in projectPlugins) plugin.Announce();
        }

        internal PluginMetadata GatherPluginMetadata()
        {
            var metadata = new PluginMetadata();
            foreach (var plugin in projectPlugins)
            {
                if (plugin is IHasMetadata m)
                {
                    m.AddMetadata(metadata);
                }
            }
            return metadata;
        }

        internal void DecommissionPlugins()
        {
            foreach (var plugin in projectPlugins) plugin.Decommission();
        }

        internal T GetPlugin<T>() where T : IProjectPlugin
        {
            return (T)projectPlugins.Single(p => p.GetType() == typeof(T));
        }

        private IProjectPlugin InstantiatePlugins(Type pluginType, PluginTools tools)
        {
            var plugin = (IProjectPlugin)Activator.CreateInstance(pluginType, args: tools)!;
            projectPlugins.Add(plugin);
            return plugin;
        }

        private void ApplyLogPrefix(IProjectPlugin plugin, PluginTools tools)
        {
            if (plugin is IHasLogPrefix hasLogPrefix)
            {
                tools.ApplyLogPrefix(hasLogPrefix.LogPrefix);
            }
        }
    }

    public interface IProjectPlugin
    {
        void Announce();
        void Decommission();
    }

    public interface IHasLogPrefix
    {
        string LogPrefix { get; }
    }

    public interface IHasMetadata
    {
        void AddMetadata(IAddMetadata metadata);
    }
}
