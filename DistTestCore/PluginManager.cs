using FileUtils;
using KubernetesWorkflow;
using Logging;
using System.Reflection;
using Utils;

namespace DistTestCore
{
    public class PluginManager
    {
        private readonly List<IProjectPlugin> projectPlugins = new List<IProjectPlugin>();

        public void DiscoverPlugins()
        {
            projectPlugins.Clear();
            var pluginTypes = PluginFinder.GetPluginTypes();
            foreach (var pluginType in pluginTypes)
            {
                var plugin = (IProjectPlugin)Activator.CreateInstance(pluginType)!;
                projectPlugins.Add(plugin);
            }
        }

        public void AnnouncePlugins(ILog log)
        {
            foreach (var plugin in projectPlugins) plugin.Announce(log);
        }

        public void InitializePlugins(IPluginTools tools)
        {
            foreach (var plugin in projectPlugins) plugin.Initialize(tools);
        }

        public void FinalizePlugins(ILog log)
        {
            foreach (var plugin in projectPlugins) plugin.Finalize(log);
        }

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return (T)projectPlugins.Single(p => p.GetType() == typeof(T));
        }
    }

    public static class PluginFinder
    {
        private static Type[]? pluginTypes = null;

        public static Type[] GetPluginTypes()
        {
            if (pluginTypes != null) return pluginTypes;

            // Reflection can be costly. Do this only once.
            FindAndLoadPluginAssemblies();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            pluginTypes = assemblies.SelectMany(a => a.GetTypes().Where(t =>
                typeof(IProjectPlugin).IsAssignableFrom(t) &&
                !t.IsAbstract)
            ).ToArray();

            return pluginTypes;
        }

        private static void FindAndLoadPluginAssemblies()
        {
            var files = Directory.GetFiles(".");
            foreach (var file in files)
            {
                var f = file.ToLowerInvariant();
                if (f.Contains("plugin") && f.EndsWith("dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        Assembly.Load(name);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to load plugin from file '{name}'.", ex);
                    }
                }
            }
        }
    }

    public interface IProjectPlugin
    {
        void Announce(ILog log);
        void Initialize(IPluginTools tools);
        void Finalize(ILog log);
    }

    public interface IPluginTools : IWorkflowTool, ILogTool, IHttpFactoryTool, IFileTool
    {        
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
        Http CreateHttp(Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null);
        Http CreateHttp(Address address, string baseUrl, string? logAlias = null);
    }
    
    public interface IFileTool
    {
        IFileManager GetFileManager();
    }
}
