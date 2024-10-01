using KubernetesWorkflow.Recipe;
using KubernetesWorkflow;
using Utils;

namespace CodexDiscordBotPlugin
{
    public class RewarderBotContainerRecipe : ContainerRecipeFactory
    {
        public override string AppName => "discordbot-rewarder";
        public override string Image => "codexstorage/codex-rewarderbot:sha-fb25372";

        protected override void Initialize(StartupConfig startupConfig)
        {
            var config = startupConfig.Get<RewarderBotStartupConfig>();

            SetSchedulingAffinity(notIn: "false");

            AddEnvVar("DISCORDBOTHOST", config.DiscordBotHost);
            AddEnvVar("DISCORDBOTPORT", config.DiscordBotPort.ToString());
            AddEnvVar("INTERVALMINUTES", config.IntervalMinutes.ToString());
            var offset = new DateTimeOffset(config.HistoryStartUtc);
            AddEnvVar("CHECKHISTORY", offset.ToUnixTimeSeconds().ToString());

            var gethInfo = config.GethInfo;
            AddEnvVar("GETH_HOST", gethInfo.Host);
            AddEnvVar("GETH_HTTP_PORT", gethInfo.Port.ToString());
            AddEnvVar("GETH_PRIVATE_KEY", gethInfo.PrivKey);
            AddEnvVar("CODEXCONTRACTS_MARKETPLACEADDRESS", gethInfo.MarketplaceAddress);
            AddEnvVar("CODEXCONTRACTS_TOKENADDRESS", gethInfo.TokenAddress);
            AddEnvVar("CODEXCONTRACTS_ABI", gethInfo.Abi);

            if (!string.IsNullOrEmpty(config.DataPath))
            {
                AddEnvVar("DATAPATH", config.DataPath);
                AddVolume(config.DataPath, 1.GB());
            }
        }
    }
}
