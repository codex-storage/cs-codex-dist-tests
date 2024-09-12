using CodexOpenApi;
using Logging;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Utils;

namespace AutoClient
{
    public class CodexUser
    {
        private readonly ILog log;
        private readonly CodexApi codex;
        private readonly HttpClient client;
        private readonly Address address;
        private readonly IFileGenerator generator;
        private readonly Configuration config;
        private readonly CancellationToken cancellationToken;

        public CodexUser(ILog log, CodexApi codex, HttpClient client, Address address, IFileGenerator generator, Configuration config, CancellationToken cancellationToken)
        {
            this.log = log;
            this.codex = codex;
            this.client = client;
            this.address = address;
            this.generator = generator;
            this.config = config;
            this.cancellationToken = cancellationToken;
        }

        public async Task Run()
        {
            var purchasers = new List<Purchaser>();
            for (var i = 0; i < config.NumConcurrentPurchases; i++)
            {
                purchasers.Add(new Purchaser(new LogPrefixer(log, $"({i}) "), client, address, codex, config, generator, cancellationToken));
            }

            var delayPerPurchaser = TimeSpan.FromMinutes(config.ContractDurationMinutes) / config.NumConcurrentPurchases;
            foreach (var purchaser in purchasers)
            {
                purchaser.Start();
                await Task.Delay(delayPerPurchaser);
            }
        }
    }
}
