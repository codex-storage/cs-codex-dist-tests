using KubernetesWorkflow;
using Logging;
using WebUtils;

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
        private readonly IWebCallTimeSet webCallTimeSet;
        private readonly IK8sTimeSet k8STimeSet;

        public ToolsFactory(ILog log, Configuration configuration, string fileManagerRootFolder, IWebCallTimeSet webCallTimeSet, IK8sTimeSet k8STimeSet)
        {
            this.log = log;
            workflowCreator = new WorkflowCreator(log, configuration);
            this.fileManagerRootFolder = fileManagerRootFolder;
            this.webCallTimeSet = webCallTimeSet;
            this.k8STimeSet = k8STimeSet;
        }

        public PluginTools CreateTools()
        {
            return new PluginTools(log, workflowCreator, fileManagerRootFolder, webCallTimeSet, k8STimeSet);
        }
    }
}
