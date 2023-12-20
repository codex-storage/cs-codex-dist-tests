using Newtonsoft.Json;
using System.Net;
using TaskFactory = Utils.TaskFactory;

namespace BiblioTech.Rewards
{
    public interface IDiscordRoleController
    {
        void GiveRole(ulong roleId, UserData userData);
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
            listener.Prefixes.Add($"http://*:31080/");
            listener.Start();
            taskFactory.Run(ConnectionDispatcher, nameof(ConnectionDispatcher));
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
                            HandleConnection(context);
                        }
                        catch (Exception ex)
                        {
                            Program.Log.Error("Exception during HTTP handler: " + ex);
                        }
                        // Whatever happens, everything's always OK.
                        context.Response.StatusCode = 200;
                        context.Response.OutputStream.Close();
                    }, nameof(HandleConnection));
                }
            }
        }

        private void HandleConnection(HttpListenerContext context)
        {
            var reader = new StreamReader(context.Request.InputStream);
            var content = reader.ReadToEnd();

            var rewards = JsonConvert.DeserializeObject<GiveRewards>(content);
            if (rewards != null) ProcessRewards(rewards);
        }

        private void ProcessRewards(GiveRewards rewards)
        {
            foreach (var reward in rewards.Rewards) ProcessReward(reward);
        }

        private void ProcessReward(Reward reward)
        {
            foreach (var userAddress in reward.UserAddresses) GiveRoleToUser(reward.RewardId, userAddress);
        }

        private void GiveRoleToUser(ulong rewardId, string userAddress)
        {
            var userData = Program.UserRepo.GetUserDataForAddress(new GethPlugin.EthAddress(userAddress));
            if (userData == null) return;

            roleController.GiveRole(rewardId, userData);
        }
    }
}
