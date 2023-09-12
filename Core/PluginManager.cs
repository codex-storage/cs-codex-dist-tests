using FileUtils;
using KubernetesWorkflow;
using Logging;
using System.Reflection;
using Utils;

namespace Core
{
    public class PluginManager
    {
        private readonly List<IProjectPlugin> projectPlugins = new List<IProjectPlugin>();

        public void InstantiatePlugins(Type[] pluginTypes)
        {
            projectPlugins.Clear();
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
