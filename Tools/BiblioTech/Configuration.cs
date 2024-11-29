using ArgsUniform;
using System.Numerics;

namespace BiblioTech
{
    public class Configuration
    {
        [Uniform("token", "t", "TOKEN", true, "Discord Application Token")]
        public string ApplicationToken { get; set; } = string.Empty;

        [Uniform("server-id", "sn", "SERVERID", true, "ID of the Discord server")] 
        public ulong ServerId { get; set; }

        [Uniform("datapath", "dp", "DATAPATH", true, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";
        
        [Uniform("admin-role-id", "a", "ADMINROLEID", true, "ID of the Discord server admin role")]
        public ulong AdminRoleId { get; set; }

        [Uniform("admin-channel-id", "ac", "ADMINCHANNELID", true, "ID of the Discord server channel where admin commands are allowed.")]
        public ulong AdminChannelId{ get; set; }

        [Uniform("rewards-channel-id", "rc", "REWARDSCHANNELID", false, "ID of the Discord server channel where participation rewards will be announced.")]
        public ulong RewardsChannelId { get; set; }

        [Uniform("chain-events-channel-id", "cc", "CHAINEVENTSCHANNELID", false, "ID of the Discord server channel where chain events will be posted.")]
        public ulong ChainEventsChannelId { get; set; }

        [Uniform("altruistic-role-id", "ar", "ALTRUISTICROLE", true, "ID of the Discord server role for Altruistic Mode.")]
        public ulong AltruisticRoleId { get; set; }

        [Uniform("reward-api-port", "rp", "REWARDAPIPORT", true, "TCP listen port for the reward API.")]
        public int RewardApiPort { get; set; } = 31080;

        [Uniform("send-eth", "se", "SENDETH", true, "Amount of Eth send by the mint command.")]
        public int SendEth { get; set; } = 10;

        [Uniform("mint-tt", "mt", "MINTTT", true, "Amount of TSTWEI minted by the mint command.")]
        public BigInteger MintTT { get; set; } = 1073741824;

        [Uniform("no-discord", "nd", "NODISCORD", false, "For debugging: Bypasses all Discord API calls.")]
        public int NoDiscord { get; set; } = 0;

        [Uniform("codex-endpoint", "ce", "CODEXENDPOINT", false, "Codex endpoint. (default 'http://localhost:8080')")]
        public string CodexEndpoint { get; set; } = "http://localhost:8080";

        [Uniform("codex-endpoint-auth", "cea", "CODEXENDPOINTAUTH", false, "Codex endpoint basic auth. Colon separated username and password. (default: empty, no auth used.)")]
        public string CodexEndpointAuth { get; set; } = "";

        public string EndpointsPath => Path.Combine(DataPath, "endpoints");
        public string UserDataPath => Path.Combine(DataPath, "users");
        public string LogPath => Path.Combine(DataPath, "logs");
        public bool DebugNoDiscord => NoDiscord == 1;
    }
}
