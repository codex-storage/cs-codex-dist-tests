using Utils;

namespace CodexClient
{
    public interface ICodexInstance
    {
        string Name { get; }
        string ImageName { get; }
        DateTime StartUtc { get; }
        Address DiscoveryEndpoint { get; }
        Address ApiEndpoint { get; }
        Address ListenEndpoint { get; }
        EthAccount? EthAccount { get; }
        Address? MetricsEndpoint { get; }
    }

    public class CodexInstance : ICodexInstance
    {
        public CodexInstance(string name, string imageName, DateTime startUtc, Address discoveryEndpoint, Address apiEndpoint, Address listenEndpoint, EthAccount? ethAccount, Address? metricsEndpoint)
        {
            Name = name;
            ImageName = imageName;
            StartUtc = startUtc;
            DiscoveryEndpoint = discoveryEndpoint;
            ApiEndpoint = apiEndpoint;
            ListenEndpoint = listenEndpoint;
            EthAccount = ethAccount;
            MetricsEndpoint = metricsEndpoint;
        }

        public string Name { get; }
        public string ImageName { get; }
        public DateTime StartUtc { get; }
        public Address DiscoveryEndpoint { get; }
        public Address ApiEndpoint { get; }
        public Address ListenEndpoint { get; }
        public EthAccount? EthAccount { get; }
        public Address? MetricsEndpoint { get; }

        public static ICodexInstance CreateFromApiEndpoint(string name, Address apiEndpoint)
        {
            return new CodexInstance(
                name,
                imageName: "-",
                startUtc: DateTime.UtcNow,
                discoveryEndpoint: Address.Empty(),
                apiEndpoint: apiEndpoint,
                listenEndpoint: Address.Empty(),
                ethAccount: null,
                metricsEndpoint: null
            );
        }
    }
}
