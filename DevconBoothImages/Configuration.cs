namespace DevconBoothImages
{
    public class Configuration
    {
        public string CodexLocalEndpoint { get; } = "http://localhost:8080";
        public string CodexPublicEndpoint { get; } = "https://api.testnet.codex.storage/storage/node-9";

        public string AuthUser { get; } = "";
        public string AuthPw { get; } = "";
        public string LocalNodeBootstrapInfo { get; } = "";
        public string WorkingDir { get; } = "D:\\DevconBoothApp";
    }
}
