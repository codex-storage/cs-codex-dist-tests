using ArgsUniform;
using CodexPlugin;
using DistTestCore;

namespace CodexNetDeployer
{
    public class Configuration
    {
        public const int SecondsIn1Day = 24 * 60 * 60;
        public const int TenMinutes = 10 * 60;

        [Uniform("deploy-name", "nm", "DEPLOYNAME", false, "Name of the deployment. (optional)")]
        public string DeploymentName { get; set; } = "unnamed";

        [Uniform("kube-config", "kc", "KUBECONFIG", false, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("kube-namespace", "kn", "KUBENAMESPACE", true, "Kubernetes namespace to be used for deployment.")]
        public string KubeNamespace { get; set; } = string.Empty;
        
        [Uniform("deploy-id", "di", "DEPLOYID", false, "ID of the deployment. (default) to current time)")]
        public string DeployId { get; set; } = NameUtils.MakeDeployId();

        [Uniform("deploy-file", "df", "DEPLOYFILE", false, "Output deployment JSON file that will be written. Defaults to 'codex-deployment.json'.")]
        public string DeployFile { get; set; } = "codex-deployment.json";

        [Uniform("codex-local-repo", "cr", "CODEXLOCALREPOPATH", false, "If set, instead of using the default Codex docker image, the local repository will be used to build an image. " +
            "This requires the 'DOCKERUSERNAME' and 'DOCKERPASSWORD' environment variables to be set. (You can omit the password to use your system default, or use a docker access token as DOCKERPASSWORD.) You can set " +
            "'DOCKERTAG' to define the image tag. If not set, one will be generated.")]
        public string CodexLocalRepoPath { get; set; } = string.Empty;

        [Uniform("nodes", "n", "NODES", true, "Number of Codex nodes to be created.")]
        public int? NumberOfCodexNodes { get; set; }

        [Uniform("validators", "v", "VALIDATORS", true, "Number of Codex nodes that will be validating.")]
        public int? NumberOfValidators { get; set; }

        [Uniform("storage-quota", "sq", "STORAGEQUOTA", true, "Storage quota in megabytes used by each Codex node.")]
        public int? StorageQuota { get; set; }

        [Uniform("log-level", "l", "LOGLEVEL", true, "Log level used by each Codex node. [Trace, Debug*, Info, Warn, Error]")]
        public CodexLogLevel CodexLogLevel { get; set; } = CodexLogLevel.Debug;
        
        [Uniform("log-level-libp2p", "lp2p", "LOGLEVELLIBP2P", true, "Log level for all libp2p topics. [Trace, Debug, Info, Warn*, Error]")]
        public CodexLogLevel Libp2pLogLevel { get; set; } = CodexLogLevel.Warn;

        [Uniform("log-level-discv5", "ldv5", "LOGLEVELDISCV5", true, "Log level for all discv5 topics. [Trace, Debug, Info, Warn*, Error]")]
        public CodexLogLevel Discv5LogLevel { get; set; } = CodexLogLevel.Warn;

        [Uniform("make-storage-available", "msa", "MAKESTORAGEAVAILABLE", true, "Is true, storage will be made available using the next configuration parameters.")]
        public bool ShouldMakeStorageAvailable { get; set; } = false;

        [Uniform("storage-sell", "ss", "STORAGESELL", false, "Number of megabytes of storage quota to make available for selling.")]
        public int? StorageSell { get; set; }

        [Uniform("test-tokens", "tt", "TESTTOKENS", false, "Initial amount of test-tokens minted for each Codex node.")]
        public int InitialTestTokens { get; set; }

        [Uniform("min-price", "mp", "MINPRICE", false, "Minimum price per byte per second in TSTWEI for the storage space for which contracts will be accepted.")]
        public int MinPricePerBytePerSecond { get; set; }

        [Uniform("max-collateral", "mc", "MAXCOLLATERAL", false, "Maximum collateral that will be placed for the total storage space.")]
        public int MaxCollateral { get; set; }

        [Uniform("max-duration", "md", "MAXDURATION", false, "Maximum duration in seconds for contracts which will be accepted.")]
        public int MaxDuration { get; set; }

        [Uniform("block-ttl", "bt", "BLOCKTTL", false, "Block timeout in seconds. Default is 24 hours.")]
        public int BlockTTL { get; set; } = SecondsIn1Day;

        [Uniform("block-mi", "bmi", "BLOCKMI", false, "Block maintenance interval in seconds. Default is 10 minutes.")]
        public int BlockMI { get; set; } = TenMinutes;

        [Uniform("block-mn", "bmn", "BLOCKMN", false, "Number of blocks maintained per interval. Default is 1000 blocks.")]
        public int BlockMN { get; set; } = 1000;

        [Uniform("metrics-endpoints", "me", "METRICSENDPOINTS", false, "[true, false]. Determines if metric endpoints will be enabled. Default is false.")]
        public bool MetricsEndpoints { get; set; } = false;

        [Uniform("metrics-scraper", "ms", "METRICSSCRAPER", false, "[true, false]. Determines if metrics scraper service will be deployed. (Required for certain tests.) Default is false.")]
        public bool MetricsScraper { get; set; } = false;

        [Uniform("teststype-podlabel", "ttpl", "TESTSTYPE-PODLABEL", false, "Each kubernetes pod will be created with a label 'teststype' with value 'continuous'. " +
            "set this option to override the label value.")]
        public string TestsTypePodLabel { get; set; } = "continuous-tests";

        [Uniform("check-connect", "cc", "CHECKCONNECT", false, "If true, deployer check ensure peer-connectivity between all deployed nodes after deployment. Default is false.")]
        public bool CheckPeerConnection { get; set; } = false;

        [Uniform("public-testnet", "ptn", "PUBLICTESTNET", false, "If true, deployment is created for public exposure. Default is false.")]
        public bool IsPublicTestNet { get; set; } = false;

        [Uniform("public-discports", "pdps", "PUBLICDISCPORTS", false, "Required if public-testnet is true. Comma-separated port numbers used for discovery. Number must match number of nodes.")]
        public string PublicDiscPorts { get; set; } = string.Empty;

        [Uniform("public-listenports", "plps", "PUBLICLISTENPORTS", false, "Required if public-testnet is true. Comma-separated port numbers used for listening. Number must match number of nodes.")]
        public string PublicListenPorts { get; set; } = string.Empty;

        [Uniform("public-gethdiscport", "pgdp", "PUBLICGETHDISCPORT", false, "Required if public-testnet is true. Single port number used for Geth's public discovery port.")]
        public int PublicGethDiscPort { get; set; }

        [Uniform("public-gethlistenport", "pglp", "PUBLICGETHLISTENPORT", false, "Required if public-testnet is true. Single port number used for Geth's public listen port.")]
        public int PublicGethListenPort { get; set; }

        [Uniform("discord-bot", "dbot", "DISCORDBOT", false, "If true, will deploy discord bot. Default is false.")]
        public bool DeployDiscordBot { get; set; } = false;

        [Uniform("dbot-token", "dbott", "DBOTTOKEN", false, "Required if discord-bot is true. Discord token used by bot.")]
        public string DiscordBotToken { get; set; } = string.Empty;

        [Uniform("dbot-servername", "dbotsn", "DBOTSERVERNAME", false, "Required if discord-bot is true. Name of the Discord server.")]
        public string DiscordBotServerName { get; set; } = string.Empty;

        [Uniform("dbot-adminrolename", "dbotarn", "DBOTADMINROLENAME", false, "Required if discord-bot is true. Name of the Discord role which will have access to admin features.")]
        public string DiscordBotAdminRoleName { get; set; } = string.Empty;

        [Uniform("dbot-adminchannelname", "dbotacn", "DBOTADMINCHANNELNAME", false, "Required if discord-bot is true. Name of the Discord channel in which admin commands are allowed.")]
        public string DiscordBotAdminChannelName { get; set; } = string.Empty;
        
        [Uniform("dbot-rewardchannelname", "dbotrcn", "DBOTREWARDCHANNELNAME", false, "Required if discord-bot is true. Name of the Discord channel in which reward updates are posted.")]
        public string DiscordBotRewardChannelName { get; set; } = string.Empty;

        [Uniform("dbot-datapath", "dbotdp", "DBOTDATAPATH", false, "Optional. Path in container where bot will save all data.")]
        public string DiscordBotDataPath { get; set; } = string.Empty;

        public List<string> Validate()
        {
            var errors = new List<string>();

            StringIsSet(nameof(KubeNamespace), KubeNamespace, errors);
            StringIsSet(nameof(KubeConfigFile), KubeConfigFile, errors);
            StringIsSet(nameof(TestsTypePodLabel), TestsTypePodLabel, errors);

            if (NumberOfValidators > NumberOfCodexNodes)
            {
                errors.Add($"{nameof(NumberOfValidators)} ({NumberOfValidators}) may not be greater than {nameof(NumberOfCodexNodes)} ({NumberOfCodexNodes}).");
            }
            if (StorageSell.HasValue && StorageQuota.HasValue && StorageSell.Value >= StorageQuota.Value)
            {
                errors.Add("StorageSell cannot be greater than or equal to StorageQuota.");
            }

            if (ShouldMakeStorageAvailable)
            {
                IntIsOverZero(nameof(StorageSell), StorageSell, errors);
                IntIsOverZero(nameof(InitialTestTokens), InitialTestTokens, errors);
                IntIsOverZero(nameof(MinPricePerBytePerSecond), MinPricePerBytePerSecond, errors);
                IntIsOverZero(nameof(MaxCollateral), MaxCollateral, errors);
                IntIsOverZero(nameof(MaxDuration), MaxDuration, errors);
            }

            if (IsPublicTestNet)
            {
                if (NumberOfCodexNodes > 0)
                {
                    if (PublicDiscPorts.Split(",").Length != NumberOfCodexNodes) errors.Add("Number of public discovery-ports provided does not match number of codex nodes.");
                    if (PublicListenPorts.Split(",").Length != NumberOfCodexNodes) errors.Add("Number of public listen-ports provided does not match number of codex nodes.");
                }
                if (PublicGethDiscPort == 0) errors.Add("Geth public discovery port is not set.");
                if (PublicGethListenPort == 0) errors.Add("Geth public listen port is not set.");
            }

            if (DeployDiscordBot)
            {
                StringIsSet(nameof(DiscordBotToken), DiscordBotToken, errors);
                StringIsSet(nameof(DiscordBotServerName), DiscordBotServerName, errors);
                StringIsSet(nameof(DiscordBotAdminRoleName), DiscordBotAdminRoleName, errors);
                StringIsSet(nameof(DiscordBotAdminChannelName), DiscordBotAdminChannelName, errors);
                StringIsSet(nameof(DiscordBotRewardChannelName), DiscordBotRewardChannelName, errors);
            }

            return errors;
        }

        private static void IntIsOverZero(string variable, int? value, List<string> errors)
        {
            if (value == null || value.Value < 1)
            {
                errors.Add($"{variable} must be set and must be greater than 0.");
            }
        }

        private static void StringIsSet(string variable, string value, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{variable} must be set.");
            }
        }
    }
}
