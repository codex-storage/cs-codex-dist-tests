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
        private readonly WorkflowCreator workflowCreator;
        private readonly string fileManagerRootFolder;
        private readonly ITimeSet timeSet;

        public ToolsFactory(ILog log, Configuration configuration, string fileManagerRootFolder, ITimeSet timeSet)
        {
            this.log = log;
            workflowCreator = new WorkflowCreator(log, configuration);
            this.fileManagerRootFolder = fileManagerRootFolder;
            this.timeSet = timeSet;
        }

        public PluginTools CreateTools()
        {
            return new PluginTools(log, workflowCreator, fileManagerRootFolder, timeSet);
        }
    }
}
