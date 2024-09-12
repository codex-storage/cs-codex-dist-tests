using CodexOpenApi;
using Logging;
using Utils;

namespace AutoClient
{
    public class CodexUser
    {
        private readonly App app;
        private readonly CodexApi codex;
        private readonly HttpClient client;
        private readonly Address address;
        private readonly List<Purchaser> purchasers = new List<Purchaser>();
        private Task starterTask = Task.CompletedTask;
        private readonly string nodeId = Guid.NewGuid().ToString();

        public CodexUser(App app, CodexApi codex, HttpClient client, Address address)
        {
            this.app = app;
            this.codex = codex;
            this.client = client;
            this.address = address;
        }

        public void Start(int index)
        {
            for (var i = 0; i < app.Config.NumConcurrentPurchases; i++)
            {
                purchasers.Add(new Purchaser(app, nodeId, new LogPrefixer(app.Log, $"({i}) "), client, address, codex));
            }

            var delayPerPurchaser =
                TimeSpan.FromSeconds(10 * index) +
                TimeSpan.FromMinutes(app.Config.ContractDurationMinutes) / app.Config.NumConcurrentPurchases;

            starterTask = Task.Run(() => StartPurchasers(delayPerPurchaser));
        }

        private async Task StartPurchasers(TimeSpan delayPerPurchaser)
        {
            foreach (var purchaser in purchasers)
            {
                purchaser.Start();
                await Task.Delay(delayPerPurchaser);
            }
        }

        public void Stop()
        {
            starterTask.Wait();
            foreach (var purchaser in purchasers)
            {
                purchaser.Stop();
            }
        }
    }
}
