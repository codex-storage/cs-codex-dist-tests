namespace Core
{
    internal class PluginManager
    {
        private readonly List<PluginToolsPair> pairs = new List<PluginToolsPair>();

        internal void InstantiatePlugins(Type[] pluginTypes, IToolsFactory provider)
        {
            pairs.Clear();
            foreach (var pluginType in pluginTypes)
            {
                var tools = provider.CreateTools();
                var plugin = InstantiatePlugins(pluginType, tools);

                ApplyLogPrefix(plugin, tools);
            }
        }

        internal void AnnouncePlugins()
        {
            foreach (var pair in pairs) pair.Plugin.Announce();
        }

        internal PluginMetadata GatherPluginMetadata()
        {
            var metadata = new PluginMetadata();
            foreach (var pair in pairs)
            {
                if (pair.Plugin is IHasMetadata m)
                {
                    m.AddMetadata(metadata);
                }
            }
            return metadata;
        }

        internal void DecommissionPlugins(bool deleteKubernetesResources, bool deleteTrackedFiles)
        {
            foreach (var pair in pairs)
            {
                pair.Plugin.Decommission();
                pair.Tools.Decommission(deleteKubernetesResources, deleteTrackedFiles);
            }
        }

        internal T GetPlugin<T>() where T : IProjectPlugin
        {
            return (T)pairs.Single(p => p.Plugin.GetType() == typeof(T)).Plugin;
        }

        private IProjectPlugin InstantiatePlugins(Type pluginType, PluginTools tools)
        {
            var plugin = (IProjectPlugin)Activator.CreateInstance(pluginType, args: tools)!;
            pairs.Add(new PluginToolsPair(plugin, tools));
            return plugin;
        }

        private void ApplyLogPrefix(IProjectPlugin plugin, PluginTools tools)
        {
            if (plugin is IHasLogPrefix hasLogPrefix)
            {
                tools.ApplyLogPrefix(hasLogPrefix.LogPrefix);
            }
        }

        private class PluginToolsPair
        {
            public PluginToolsPair(IProjectPlugin plugin, IPluginTools tools)
            {
                Plugin = plugin;
                Tools = tools;
            }

            public IProjectPlugin Plugin { get; }
            public IPluginTools Tools { get; }
        }
    }
}
