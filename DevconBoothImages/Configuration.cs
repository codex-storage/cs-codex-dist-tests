namespace DevconBoothImages
{
    public class Configuration
    {
        public string[] CodexEndpoints { get; } =
        [
            "aaaa",
            "bbbb"
        ];

        public string AuthUser { get; } = "";
        public string AuthPw { get; } = "";
        public string LocalNodeBootstrapInfo { get; } = "";
        public string WorkingDir { get; } = "D:\\DevconBoothApp";
    }
}
