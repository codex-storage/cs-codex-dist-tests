using CodexContractsPlugin.Marketplace;
using DiscordRewards;
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
            var result = await HttpGet();
            log.Log("Is DiscordBot online: " + result);
            return result == "Pong";
        }

        public async Task<bool> SendRewards(GiveRewardsCommand command)
        {
            if (command == null || command.Rewards == null || !command.Rewards.Any()) return false;
            return await HttpPost(JsonConvert.SerializeObject(command)) == "OK";
        }

        private async Task<string> HttpGet()
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync(GetUrl());
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return string.Empty;
            }
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
            return $"{configuration.DiscordHost}:{configuration.DiscordPort}/api/reward";
        }
    }
}
