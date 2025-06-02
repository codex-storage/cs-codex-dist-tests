using ArgsUniform;

namespace AutoClient
{
    public class Configuration
    {
        [Uniform("codex-endpoints", "ce", "CODEXENDPOINTS", false, "Codex endpoints. Semi-colon separated. (default 'http://localhost:8080')")]
        public string CodexEndpoints { get; set; } =
            "http://localhost:8080" + ";" +
            "http://localhost:8081" + ";" +
            "http://localhost:8082" + ";" +
            "http://localhost:8083" + ";" +
            "http://localhost:8084" + ";" +
            "http://localhost:8085" + ";" +
            "http://localhost:8086" + ";" +
            "http://localhost:8087";

        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";

        [Uniform("purchases", "np", "PURCHASES", false, "Number of concurrent purchases.")]
        public int NumConcurrentPurchases { get; set; } = 10;

        [Uniform("contract-duration", "cd", "CONTRACTDURATION", false, "contract duration in minutes. (default 6 hours)")]
        public int ContractDurationMinutes { get; set; } =
            60 * 24 * 6; // 6 days
            //60 * 6; 6 hours
        // Cluster nodes configured for max 7-day storage.

        [Uniform("contract-expiry", "ce", "CONTRACTEXPIRY", false, "contract expiry in minutes. (default 15 minutes)")]
        public int ContractExpiryMinutes { get; set; } = 15;

        [Uniform("num-hosts", "nh", "NUMHOSTS", false, "Number of hosts for contract. (default 10)")]
        public int NumHosts { get; set; } = 5;

        [Uniform("num-hosts-tolerance", "nt", "NUMTOL", false, "Number of host tolerance for contract. (default 5)")]
        public int HostTolerance { get; set; } = 1;

        [Uniform("price","p", "PRICE", false, "Price per byte per second in TSTWEI. (default 1000)")]
        public int PricePerBytePerSecond { get; set; } = 1000;

        [Uniform("collateral", "c", "COLLATERAL", false, "Required collateral per byte in TSTWEI. (default 1)")]
        public int CollateralPerByte { get; set; } = 1;

        [Uniform("filesizemb", "smb", "FILESIZEMB", false, "When greater than zero, size of file generated and uploaded. When zero, random images are used instead.")]
        public int FileSizeMb { get; set; } = 0;

        [Uniform("folderToStore", "fts", "FOLDERTOSTORE", false, "When set, autoclient will attempt to upload and purchase storage for every non-JSON file in the provided folder.")]
        public string FolderToStore { get; set; } = "/data/EthereumMainnetPreMergeEraFiles";

        [Uniform("ethAddressFile", "eaf", "ETHADDRESSFILE", false, "File(s) with eth address used by codex node. Used for balance checking if geth/contracts information is provided. Semi-colon separated.")]
        public string EthAddressFile { get; set; } = 
            "/root/codex-testnet-starter/scripts/eth.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_2.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_3.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_4.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_5.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_6.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_7.address" + ";" +
            "/root/codex-testnet-starter/scripts/eth_8.address";

        [Uniform("slowModeDelayMinutes", "smdm", "SLOWMODEDELAYMINUTES", false, "When contract failure threshold is reached, slow down process for each file by this amount of minutes.")]
        public int SlowModeDelayMinutes { get; set; } = 30 * 1;

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }
    }
}
