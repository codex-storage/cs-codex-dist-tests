using Core;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public static class PluginInterfaceExtensions
    {
        public static RunningContainers[] StartCodexNodes(this PluginInterface pluginInterface, int number, Action<ICodexSetup> setup)
        {
            return Plugin(pluginInterface).StartCodexNodes(number, setup);
        }

        public static ICodexNodeGroup WrapCodexContainers(this PluginInterface pluginInterface, RunningContainers[] containers)
        {
            return Plugin(pluginInterface).WrapCodexContainers(containers);
        }

        public static IOnlineCodexNode SetupCodexNode(this PluginInterface pluginInterface, Action<ICodexSetup> setup)
        {
            return Plugin(pluginInterface).SetupCodexNode(setup);
        }

        public static ICodexNodeGroup SetupCodexNodes(this PluginInterface pluginInterface, int number, Action<ICodexSetup> setup)
        {
            return Plugin(pluginInterface).SetupCodexNodes(number, setup);
        }

        public static ICodexNodeGroup SetupCodexNodes(this PluginInterface pluginInterface, int number)
        {
            return Plugin(pluginInterface).SetupCodexNodes(number);
        }

        private static CodexPlugin Plugin(PluginInterface pluginInterface)
        {
            return pluginInterface.GetPlugin<CodexPlugin>();
        }
    }
}
