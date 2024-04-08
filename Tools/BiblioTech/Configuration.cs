using ArgsUniform;

namespace BiblioTech
{
    public class Configuration
    {
        [Uniform("token", "t", "TOKEN", true, "Discord Application Token")]
        public string ApplicationToken { get; set; } = string.Empty;

        [Uniform("server-name", "sn", "SERVERNAME", true, "Name of the Discord server")]
        public string ServerName { get; set; } = string.Empty;

        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";
        
        [Uniform("admin-role", "a", "ADMINROLE", true, "Name of the Discord server admin role")]
        public string AdminRoleName { get; set; } = string.Empty;

        [Uniform("admin-channel-name", "ac", "ADMINCHANNELNAME", true, "Name of the Discord server channel where admin commands are allowed.")]
        public string AdminChannelName { get; set; } = "admin-channel";

        [Uniform("rewards-channel-name", "rc", "REWARDSCHANNELNAME", false, "Name of the Discord server channel where participation rewards will be announced.")]
        public string RewardsChannelName { get; set; } = "";

        [Uniform("chain-events-channel-name", "cc", "CHAINEVENTSCHANNELNAME", false, "Name of the Discord server channel where chain events will be posted.")]
        public string ChainEventsChannelName { get; set; } = "";

        [Uniform("reward-api-port", "rp", "REWARDAPIPORT", false, "TCP listen port for the reward API.")]
        public int RewardApiPort { get; set; } = 31080;

        [Uniform("send-eth", "se", "SENDETH", false, "Amount of Eth send by the mint command. Default: 10.")]
        public int SendEth { get; set; } = 10;

        [Uniform("mint-tt", "mt", "MINTTT", false, "Amount of TestTokens minted by the mint command. Default: 1073741824")]
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
