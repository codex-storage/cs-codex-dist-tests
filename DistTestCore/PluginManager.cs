using KubernetesWorkflow;
using Logging;

namespace DistTestCore
{
    public class PluginManager : IPluginActions
    {
        private readonly BaseLog log;
        private readonly Configuration configuration;
        private readonly string testNamespace;
        private readonly WorkflowCreator workflowCreator;
        private readonly ITimeSet timeSet;
        private readonly List<IProjectPlugin> projectPlugins = new List<IProjectPlugin>();

        public PluginManager(BaseLog log, Configuration configuration, ITimeSet timeSet, string testNamespace)
        {
            this.log = log;
            this.configuration = configuration;
            this.timeSet = timeSet;
            this.testNamespace = testNamespace;
            workflowCreator = new WorkflowCreator(log, configuration.GetK8sConfiguration(timeSet), testNamespace);
        }

        public IStartupWorkflow CreateWorkflow()
        {
            return workflowCreator.CreateWorkflow();
        }

        public ILog GetLog()
        {
            return log;
        }

        public ITimeSet GetTimeSet()
        {
            return timeSet;
        }

        public void InitializeAllPlugins()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var pluginTypes = assemblies.SelectMany(a => a.GetTypes().Where(t => typeof(IProjectPlugin).IsAssignableFrom(t))).ToArray();

            foreach (var pluginType in pluginTypes)
            {
                IPluginActions actions = this;
                var plugin = (IProjectPlugin)Activator.CreateInstance(pluginType, args: actions)!;
                projectPlugins.Add(plugin);
            }
        }
    }

    public interface IProjectPlugin
    {
    }

    // probably seggregate this out.
    public interface IPluginActions
    {
        IStartupWorkflow CreateWorkflow();
        ILog GetLog();
        ITimeSet GetTimeSet();
    }
}
