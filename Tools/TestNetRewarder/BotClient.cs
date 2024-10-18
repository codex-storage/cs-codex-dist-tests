using DiscordRewards;
using Logging;
using System.Net.Http.Json;

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
            return result == "Pong";
        }

        public async Task<bool> SendRewards(GiveRewardsCommand command)
        {
            if (command == null) return false;
            var result = await HttpPostJson(command);
            log.Log("Reward response: " + result);
            return result == "OK";
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

        private async Task<string> HttpPostJson<T>(T body)
        {
            try
            {
                using var client = new HttpClient();
                using var content = JsonContent.Create(body);
                using var response = await client.PostAsync(GetUrl(), content);
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
