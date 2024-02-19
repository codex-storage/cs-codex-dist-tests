using DiscordRewards;
using Newtonsoft.Json;
using System.Net;
using TaskFactory = Utils.TaskFactory;

namespace BiblioTech.Rewards
{
    public interface IDiscordRoleController
    {
        Task GiveRewards(GiveRewardsCommand rewards);
    }

    public class RewardsApi
    {
        private readonly HttpListener listener = new HttpListener();
        private readonly TaskFactory taskFactory = new TaskFactory();
        private readonly IDiscordRoleController roleController;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public RewardsApi(IDiscordRoleController roleController)
        {
            this.roleController = roleController;
        }

        public void Start()
        {
            cts = new CancellationTokenSource();
            var uri = $"http://*:{Program.Config.RewardApiPort}/";
            listener.Prefixes.Add(uri);
            listener.Start();
            taskFactory.Run(ConnectionDispatcher, nameof(ConnectionDispatcher));
            Program.Log.Log($"Reward API listening on '{uri}'");
        }

        public void Stop()
        {
            listener.Stop();
            cts.Cancel();
            taskFactory.WaitAll();
        }

        private void ConnectionDispatcher()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var wait = listener.GetContextAsync();
                wait.Wait(cts.Token);
                if (wait.IsCompletedSuccessfully)
                {
                    taskFactory.Run(() =>
                    {
                        var context = wait.Result;
                        try
                        {
                            HandleConnection(context).Wait();
                        }
                        catch (Exception ex)
                        {
                            Program.Log.Error("Exception during HTTP handler: " + ex);
                        }
                    }, nameof(HandleConnection));
                }
            }
        }

        private async Task HandleConnection(HttpListenerContext context)
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var content = reader.ReadToEnd();

            if (content == "Ping")
            {
                using var writer = new StreamWriter(context.Response.OutputStream);
                writer.Write("Pong");
                return;
            }

            if (!content.StartsWith("{")) return;
            var rewards = JsonConvert.DeserializeObject<GiveRewardsCommand>(content);
            if (rewards != null)
            {
                await ProcessRewards(rewards);
            }
        }

        private async Task ProcessRewards(GiveRewardsCommand rewards)
        {
            await roleController.GiveRewards(rewards);
        }
    }
}
