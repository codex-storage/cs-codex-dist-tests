using BiblioTech.Rewards;
using Discord;
using DiscordRewards;
using Logging;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class LoggingRoleDriver : IDiscordRoleDriver
    {
        private readonly ILog log;

        public LoggingRoleDriver(ILog log)
        {
            this.log = log;
        }

        public async Task GiveAltruisticRole(IUser user)
        {
            await Task.CompletedTask;

            log.Log($"Give altruistic role to {user.Id}");
        }

        public async Task GiveRewards(GiveRewardsCommand rewards)
        {
            await Task.CompletedTask;

            log.Log(JsonConvert.SerializeObject(rewards, Formatting.None));
        }
    }
}
