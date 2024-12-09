using Discord;
using DiscordRewards;
using Microsoft.AspNetCore.Mvc;

namespace BiblioTech.Rewards
{
    public interface IDiscordRoleDriver
    {
        Task GiveRewards(GiveRewardsCommand rewards);
        Task GiveAltruisticRole(IUser user);
    }

    [Route("api/[controller]")]
    [ApiController]
    public class RewardController : ControllerBase
    {
        [HttpGet]
        public string Ping()
        {
            return "Pong";
        }

        [HttpPost]
        public async Task<string> Give(GiveRewardsCommand cmd)
        {
            try
            {
                await Program.RoleDriver.GiveRewards(cmd);
            }
            catch (Exception ex)
            {
                Program.Log.Error("Exception: " + ex);
            }
            return "OK";
        }
    }
}
