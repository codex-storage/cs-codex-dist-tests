using ArgsUniform;

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
        public int SendEth { get; set; } = 10;

        [Uniform("mint-tt", "mt", "MINTTT", true, "Amount of TestTokens minted by the mint command.")]
        public int MintTT { get; set; } = 1073741824;

        public string EndpointsPath
        {
            get
            {
                return Path.Combine(DataPath, "endpoints");
            }
        }

        public string UserDataPath
        {
            get
            {
                return Path.Combine(DataPath, "users");
            }
        }

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }
    }
}
