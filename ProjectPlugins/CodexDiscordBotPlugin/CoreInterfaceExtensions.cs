using Core;
using KubernetesWorkflow.Types;

namespace CodexDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainers DeployCodexDiscordBot(this CoreInterface ci, DiscordBotStartupConfig config)
        {
            return Plugin(ci).Deploy(config);
        }

        private static CodexDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexDiscordBotPlugin>();
        }
    }
}
