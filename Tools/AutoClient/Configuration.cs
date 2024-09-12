using ArgsUniform;

namespace AutoClient
{
    public class Configuration
    {
        [Uniform("codex-endpoints", "ce", "CODEXENDPOINTS", false, "Codex endpoints. Semi-colon separated. (default 'http://localhost:8080')")]
        public string CodexEndpoints { get; set; } = "http://localhost";

        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";

        [Uniform("purchases", "np", "PURCHASES", false, "Number of concurrent purchases.")]
        public int NumConcurrentPurchases { get; set; } = 10;

        [Uniform("contract-duration", "cd", "CONTRACTDURATION", false, "contract duration in minutes. (default 6 hours)")]
        public int ContractDurationMinutes { get; set; } = 60 * 6;

        [Uniform("contract-expiry", "ce", "CONTRACTEXPIRY", false, "contract expiry in minutes. (default 15 minutes)")]
        public int ContractExpiryMinutes { get; set; } = 15;

        [Uniform("num-hosts", "nh", "NUMHOSTS", false, "Number of hosts for contract. (default 5)")]
        public int NumHosts { get; set; } = 5;

        [Uniform("num-hosts-tolerance", "nt", "NUMTOL", false, "Number of host tolerance for contract. (default 2)")]
        public int HostTolerance { get; set; } = 2;

        [Uniform("price","p", "PRICE", false, "Price of contract. (default 10)")]
        public int Price { get; set; } = 10;

        [Uniform("collateral", "c", "COLLATERAL", false, "Required collateral. (default 1)")]
        public int RequiredCollateral { get; set; } = 1;

        [Uniform("filesizemb", "smb", "FILESIZEMB", false, "When greater than zero, size of file generated and uploaded. When zero, random images are used instead.")]
        public int FileSizeMb { get; set; } = 0;

        public string LogPath
        {
            get
            {
                return Path.Combine(DataPath, "logs");
            }
        }
    }
}
