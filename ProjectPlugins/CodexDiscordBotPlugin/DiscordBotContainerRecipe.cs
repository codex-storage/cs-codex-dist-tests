using KubernetesWorkflow;

namespace CodexDiscordBotPlugin
{
    public class DiscordBotContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "discordbot-bibliotech";
        public override string Image => "thatbenbierens/codex-discordbot:initial";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<DiscordBotStartupConfig>();

            AddEnvVar("TOKEN", config.Token);
            AddEnvVar("SERVERNAME", config.ServerName);
            AddEnvVar("ADMINROLE", config.AdminRoleName);
            AddEnvVar("ADMINCHANNELNAME", config.AdminChannelName);
        }
    }
}
