using Discord;
using DiscordRewards;
using Microsoft.AspNetCore.Mvc;

namespace BiblioTech.Rewards
{
    /// <summary>
    /// We like callbacks in this interface because we're trying to batch role-modifying operations,
    /// So that we're not poking the server lots of times very quickly.
    /// </summary>
    public interface IDiscordRoleDriver
    {
        Task RunRoleGiver(Func<IRoleGiver, Task> action);
        Task IterateRemoveActiveP2pParticipants(Func<IUser, bool> predicate);
    }

    public interface IRoleGiver
    {
        Task GiveAltruisticRole(IUser user);
        Task GiveActiveP2pParticipant(IUser user);
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
        public async Task<string> Give(EventsAndErrors cmd)
        {
            try
            {
                await Program.EventsSender.ProcessChainEvents(cmd.EventsOverview, cmd.Errors);
            }
            catch (Exception ex)
            {
                Program.Log.Error("Exception: " + ex);
            }
            return "OK";
        }
    }
}
