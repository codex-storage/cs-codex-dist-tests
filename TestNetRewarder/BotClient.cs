using BiblioTech.Rewards;
using Logging;
using Newtonsoft.Json;

namespace TestNetRewarder
{
    public class BotClient
    {
        private readonly Configuration configuration;
        private readonly ILog log;

        public BotClient(Configuration configuration, ILog log)
        {
            this.configuration = configuration;
            this.log = log;
        }

        public async Task<bool> IsOnline()
        {
            return await HttpPost("Ping") == "Ping";
        }

        public async Task SendRewards(GiveRewardsCommand command)
        {
            if (command == null || command.Rewards == null || !command.Rewards.Any()) return;
            await HttpPost(JsonConvert.SerializeObject(command));
        }

        private async Task<string> HttpPost(string content)
        {
            try
            {
                var client = new HttpClient();
                var response = await client.PostAsync(GetUrl(), new StringContent(content));
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return string.Empty;
            }
        }

        private string GetUrl()
        {
            return $"{configuration.DiscordHost}:{configuration.DiscordPort}";
        }
    }
}
