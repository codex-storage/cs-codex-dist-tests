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
        Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, params ulong[] rolesToIterate);
        Task IterateUsersWithRoles(Func<IRoleGiver, IUser, ulong, Task> onUserWithRole, Func<IRoleGiver, Task> whenDone, params ulong[] rolesToIterate);
    }

    public interface IRoleGiver
    {
        Task GiveAltruisticRole(ulong userId);
        Task GiveActiveP2pParticipant(ulong userId);
        Task RemoveActiveP2pParticipant(ulong userId);
        Task GiveActiveHost(ulong userId);
        Task RemoveActiveHost(ulong userId);
        Task GiveActiveClient(ulong userId);
        Task RemoveActiveClient(ulong userId);
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
            await Safe(() => Program.ChainActivityHandler.ProcessChainActivity(cmd.ActiveChainAddresses));
            await Safe(() => Program.EventsSender.ProcessChainEvents(cmd.EventsOverview, cmd.Errors));
            return "OK";
        }

        private async Task Safe(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Program.Log.Error("Exception: " + ex);
            }
        }
    }
}
