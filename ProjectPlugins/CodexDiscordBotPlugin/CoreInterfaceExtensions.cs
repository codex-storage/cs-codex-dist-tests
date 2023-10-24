using Core;
using KubernetesWorkflow;

namespace CodexDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningContainer DeployMetricsCollector(this CoreInterface ci)
        {
            return Plugin(ci).Deploy();
        }

        private static CodexDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexDiscordBotPlugin>();
        }
    }
}
