using Core;
using KubernetesWorkflow;

namespace CodexDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainer DeployCodexDiscordBot(this CoreInterface ci, DiscordBotStartupConfig config)
        {
            return Plugin(ci).Deploy(config);
        }

        private static CodexDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexDiscordBotPlugin>();
        }
    }
}
