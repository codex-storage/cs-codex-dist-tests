using FileUtils;
using KubernetesWorkflow;
using Logging;
using Utils;

namespace Core
{
    public class PluginManager
    {
        private readonly List<IProjectPlugin> projectPlugins = new List<IProjectPlugin>();

        public void InstantiatePlugins(Type[] pluginTypes, IPluginTools tools)
        {
            projectPlugins.Clear();
            foreach (var pluginType in pluginTypes)
            {
                var plugin = (IProjectPlugin)Activator.CreateInstance(pluginType, args: tools)!;
                projectPlugins.Add(plugin);
            }
        }

        public void AnnouncePlugins()
        {
            foreach (var plugin in projectPlugins) plugin.Announce();
        }

        public void DecommissionPlugins()
        {
            foreach (var plugin in projectPlugins) plugin.Decommission();
        }

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return (T)projectPlugins.Single(p => p.GetType() == typeof(T));
        }
    }

    public interface IProjectPlugin
    {
        void Announce();
        void Decommission();
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
