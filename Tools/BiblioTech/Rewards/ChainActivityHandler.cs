using Discord;
using DiscordRewards;
using Logging;

namespace BiblioTech.Rewards
{
    public class ChainActivityHandler
    {
        private readonly ILog log;
        private readonly UserRepo repo;
        private ActiveUserIds? previousIds = null;

        public ChainActivityHandler(ILog log, UserRepo repo)
        {
            this.log = log;
            this.repo = repo;
        }

        public async Task ProcessChainActivity(ActiveChainAddresses activeChainAddresses)
        {
            if (!activeChainAddresses.HasAny()) return;
            var activeUserIds = ConvertToUserIds(activeChainAddresses);
            if (!activeUserIds.HasAny()) return;
            if (!HasChanged(activeUserIds)) return;
            await GiveAndRemoveRoles(activeUserIds);
        }

        private async Task GiveAndRemoveRoles(ActiveUserIds activeUserIds)
        {
            await Program.RoleDriver.IterateUsersWithRoles(
                (g, u, r) => OnUserWithRole(g, u, r, activeUserIds),
                whenDone: g => GiveRolesToRemaining(g, activeUserIds),
                Program.Config.ActiveClientRoleId,
                Program.Config.ActiveHostRoleId);
        }

        private async Task OnUserWithRole(IRoleGiver giver, IUser user, ulong roleId, ActiveUserIds activeIds)
        {
            if (roleId == Program.Config.ActiveClientRoleId)
            {
                await CheckUserWithRole(user, activeIds.Clients, giver.RemoveActiveClient);
            }
            else if (roleId == Program.Config.ActiveHostRoleId)
            {
                await CheckUserWithRole(user, activeIds.Hosts, giver.RemoveActiveHost);
            }
            else
            {
                throw new Exception("Unknown roleId received!");
            }
        }

        private async Task CheckUserWithRole(IUser user, List<ulong> activeUsers, Func<ulong, Task> removeActiveRole)
        {
            if (ShouldUserHaveRole(user, activeUsers))
            {
                activeUsers.Remove(user.Id);
            }
            else
            {
                await removeActiveRole(user.Id);
            }
        }

        private bool ShouldUserHaveRole(IUser user, List<ulong> activeUsers)
        {
            return activeUsers.Any(id => id == user.Id);
        }

        private async Task GiveRolesToRemaining(IRoleGiver giver, ActiveUserIds ids)
        {
            foreach (var client in ids.Clients) await giver.GiveActiveClient(client);
            foreach (var host in ids.Hosts) await giver.GiveActiveHost(host);
        }

        private bool HasChanged(ActiveUserIds activeUserIds)
        {
            if (previousIds == null)
            {
                previousIds = activeUserIds;
                return true;
            }
            
            if (!IsEquivalent(previousIds.Hosts, activeUserIds.Hosts)) return true;
            if (!IsEquivalent(previousIds.Clients, activeUserIds.Clients)) return true;
            return false;
        }

        private static bool IsEquivalent(IEnumerable<ulong> a, IEnumerable<ulong> b)
        {
            return a.SequenceEqual(b);
        }

        private ActiveUserIds ConvertToUserIds(ActiveChainAddresses activeChainAddresses)
        {
            return new ActiveUserIds
            (
                hosts: Map(activeChainAddresses.Hosts),
                clients: Map(activeChainAddresses.Clients)
            );
        }

        private ulong[] Map(string[] ethAddresses)
        {
            var result = new List<ulong>();
            foreach (var ethAddress in ethAddresses)
            {
                var userMaybe = repo.GetUserDataForAddressMaybe(new Utils.EthAddress(ethAddress));
                if (userMaybe != null)
                {
                    result.Add(userMaybe.DiscordId);
                }
            }

            return result.Order().ToArray();
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }

        private class ActiveUserIds
        {
            public ActiveUserIds(IEnumerable<ulong> hosts, IEnumerable<ulong> clients)
            {
                Hosts = hosts.ToList();
                Clients = clients.ToList();
            }

            public List<ulong> Hosts { get; }
            public List<ulong> Clients { get; }

            public bool HasAny()
            {
                return Hosts.Any() || Clients.Any();
            }

            public override string ToString()
            {
                return "Hosts:" + string.Join(",", Hosts) + "Clients:" + string.Join(",", Clients);
            }
        }
    }
}
