﻿using Core;
using KubernetesWorkflow.Types;

namespace CodexDiscordBotPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static RunningPod DeployCodexDiscordBot(this CoreInterface ci, DiscordBotStartupConfig config)
        {
            return Plugin(ci).Deploy(config);
        }

        public static RunningPod DeployRewarderBot(this CoreInterface ci, RewarderBotStartupConfig config)
        {
            return Plugin(ci).DeployRewarder(config);
        }

        private static CodexDiscordBotPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<CodexDiscordBotPlugin>();
        }
    }
}
