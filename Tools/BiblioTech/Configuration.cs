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

        [Uniform("reward-api-port", "rp", "REWARDAPIPORT", true, "TCP listen port for the reward API.")]
        public int RewardApiPort { get; set; } = 31080;

        [Uniform("send-eth", "se", "SENDETH", true, "Amount of Eth send by the mint command.")]
        public decimal SendEth { get; set; } = 10.0m;

        [Uniform("mint-tt", "mt", "MINTTT", true, "Amount of TSTWEI minted by the mint command.")]
        public BigInteger MintTT { get; set; } = 1073741824;

        [Uniform("send-tt", "st", "SENDTT", true, "Amount of TSTWEI sent from the bot account by the mint command.")]
        public BigInteger SendTT { get; set; } = 1073741824;

        [Uniform("no-discord", "nd", "NODISCORD", false, "For debugging: Bypasses all Discord API calls.")]
        public int NoDiscord { get; set; } = 0;

        [Uniform("codex-endpoint", "ce", "CODEXENDPOINT", false, "Codex endpoint. (default 'http://localhost:8080')")]
        public string CodexEndpoint { get; set; } = "http://localhost:8080";

        [Uniform("codex-endpoint-auth", "cea", "CODEXENDPOINTAUTH", false, "Codex endpoint basic auth. Colon separated username and password. (default: empty, no auth used.)")]
        public string CodexEndpointAuth { get; set; } = "";

        [Uniform("transaction-link-format", "tlf", "TRANSACTIONLINKFORMAT", false, "Format of links to transactions on the blockchain. Use '<ID>' to inject the transaction ID into this string. (default 'https://explorer.testnet.codex.storage/tx/<ID>')")]
        public string TransactionLinkFormat { get; set; } = "https://explorer.testnet.codex.storage/tx/<ID>";

        #region Role Rewards

        /// <summary>
        /// Awarded when both checkupload and checkdownload have been completed.
        /// </summary>
        [Uniform("altruistic-role-id", "ar", "ALTRUISTICROLE", true, "ID of the Discord server role for Altruistic Mode.")]
        public ulong AltruisticRoleId { get; set; }

        /// <summary>
        /// Awarded as long as either checkupload or checkdownload were completed within the last ActiveP2pRoleDuration minutes.
        /// </summary>
        [Uniform("active-p2p-role-id", "apri", "ACTIVEP2PROLEID", false, "ID of discord server role for active p2p participants.")]
        public ulong ActiveP2pParticipantRoleId { get; set; }

        [Uniform("active-p2p-role-duration", "aprd", "ACTIVEP2PROLEDURATION", false, "Duration in minutes for the active p2p participant role from the last successful check command.")]
        public int ActiveP2pRoleDurationMinutes { get; set; }

        /// <summary>
        /// Awarded as long as the user is hosting at least 1 slot.
        /// </summary>
        [Uniform("active-host-role-id", "ahri", "ACTIVEHOSTROLEID", false, "Id of discord server role for active slot hosters.")]
        public ulong ActiveHostRoleId { get; set; }

        /// <summary>
        /// Awarded as long as the user has at least 1 active storage purchase contract.
        /// </summary>
        [Uniform("active-client-role-id", "acri", "ACTIVECLIENTROLEID", false, "Id of discord server role for users with at least 1 active purchase contract.")]
        public ulong ActiveClientRoleId { get; set; }

        #endregion

        public string EndpointsPath => Path.Combine(DataPath, "endpoints");
        public string UserDataPath => Path.Combine(DataPath, "users");
        public string ChecksDataPath => Path.Combine(DataPath, "checks");
        public string LogPath => Path.Combine(DataPath, "logs");
        public bool DebugNoDiscord => NoDiscord == 1;
    }
}
