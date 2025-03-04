namespace GethConnector
{
    public static class GethInput
    {
        private const string GethHostVar = "GETH_HOST";
        private const string GethPortVar = "GETH_HTTP_PORT";
        private const string GethPrivKeyVar = "GETH_PRIVATE_KEY";
        private const string MarketplaceAddressVar = "CODEXCONTRACTS_MARKETPLACEADDRESS";
        private const string TokenAddressVar = "CODEXCONTRACTS_TOKENADDRESS";
        private const string AbiVar = "CODEXCONTRACTS_ABI";

        static GethInput()
        {
            var error = new List<string>();
            var gethHost = GetEnvVar(error, GethHostVar);
            var gethPort = Convert.ToInt32(GetEnvVar(error, GethPortVar));
            var privateKey = GetEnvVar(error, GethPrivKeyVar);
            var marketplaceAddress = GetEnvVar(error, MarketplaceAddressVar);
            var tokenAddress = GetEnvVar(error, TokenAddressVar);
            var abi = GetEnvVar(error, AbiVar);

            if (error.Any())
            {
                LoadError = string.Join(", ", error);
            }
            else
            {
                GethHost = gethHost!;
                GethPort = gethPort;
                PrivateKey = privateKey!;
                MarketplaceAddress = marketplaceAddress!;
                TokenAddress = tokenAddress!;
                ABI = abi!;
            }
        }

        public static string GethHost { get; } = string.Empty;
        public static int GethPort { get; }
        public static string PrivateKey { get; } = string.Empty;
        public static string MarketplaceAddress { get; } = string.Empty;
        public static string TokenAddress { get; } = string.Empty;
        public static string ABI { get; } = string.Empty;
        public static string LoadError { get; } = string.Empty;

        private static string? GetEnvVar(List<string> error, string name)
        {
            var result = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(result)) error.Add($"'{name}' is not set.");
            return result.Trim();
        }
    }
}
