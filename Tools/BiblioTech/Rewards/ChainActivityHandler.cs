using DiscordRewards;
using Logging;

namespace BiblioTech.Rewards
{
    public class ChainActivityHandler
    {
        private readonly ILog log;
        private readonly UserRepo repo;

        public ChainActivityHandler(ILog log, UserRepo repo)
        {
            this.log = log;
            this.repo = repo;
        }

        public async Task Process(ActiveChainAddresses activeChainAddresses)
        {
            var activeUserIds = ConvertToUserIds(activeChainAddresses);
            if (!activeUserIds.HasAny()) return;

            todo call role driver to add roles to new activeIds or remove them.
        }

        private ActiveUserIds ConvertToUserIds(ActiveChainAddresses activeChainAddresses)
        {
            return new ActiveUserIds
            {
                Hosts = Map(activeChainAddresses.Hosts),
                Clients = Map(activeChainAddresses.Clients)
            };
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

            return result.ToArray();
        }

        private class ActiveUserIds
        {
            public ulong[] Hosts { get; set; } = Array.Empty<ulong>();
            public ulong[] Clients { get; set; } = Array.Empty<ulong>();

            public bool HasAny()
            {
                return Hosts.Any() || Clients.Any();
            }
        }
    }
}
