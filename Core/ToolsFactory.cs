using KubernetesWorkflow;
using Logging;

namespace Core
{
    internal interface IToolsFactory
    {
        PluginTools CreateTools();
    }

    internal class ToolsFactory : IToolsFactory
    {
        private readonly ILog log;
        private readonly Configuration configuration;
        private readonly string fileManagerRootFolder;
        private readonly ITimeSet timeSet;

        public ToolsFactory(ILog log, Configuration configuration, string fileManagerRootFolder, ITimeSet timeSet)
        {
            this.log = log;
            this.configuration = configuration;
            this.fileManagerRootFolder = fileManagerRootFolder;
            this.timeSet = timeSet;
        }

        public PluginTools CreateTools()
        {
            return new PluginTools(log, configuration, fileManagerRootFolder, timeSet);
        }
    }
}
