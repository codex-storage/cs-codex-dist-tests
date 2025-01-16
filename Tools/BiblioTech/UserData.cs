using CodexContractsPlugin;
using GethPlugin;
using Utils;

namespace BiblioTech
{
    public class UserData
    {
        public UserData(ulong discordId, string name, DateTime createdUtc, EthAddress? currentAddress, List<UserAssociateAddressEvent> associateEvents, List<UserMintEvent> mintEvents, bool notificationsEnabled)
        {
            DiscordId = discordId;
            Name = name;
            CreatedUtc = createdUtc;
            CurrentAddress = currentAddress;
            AssociateEvents = associateEvents;
            MintEvents = mintEvents;
            NotificationsEnabled = notificationsEnabled;
        }

        public ulong DiscordId { get; }
        public string Name { get; }
        public DateTime CreatedUtc { get; }
        public EthAddress? CurrentAddress { get; set; }
        public List<UserAssociateAddressEvent> AssociateEvents { get; }
        public List<UserMintEvent> MintEvents { get; }
        public bool NotificationsEnabled { get; set; }

        public string[] CreateOverview()
        {
            return new[]
            {
                $"name: '{Name}' - id:{DiscordId}",
                $"joined: {CreatedUtc.ToString("o")}",
                $"current address: {CurrentAddress}",
                $"{AssociateEvents.Count + MintEvents.Count} total bot events."
            };
        }
    }

    public class UserAssociateAddressEvent
    {
        public UserAssociateAddressEvent(DateTime utc, EthAddress? newAddress)
        {
            Utc = utc;
            NewAddress = newAddress;
        }

        public DateTime Utc { get; }
        public EthAddress? NewAddress { get; }
    }

    public class UserMintEvent
    {
        public UserMintEvent(DateTime utc, EthAddress usedAddress, Transaction<Ether>? ethReceived, Transaction<TestToken>? testTokensMinted)
        {
            Utc = utc;
            UsedAddress = usedAddress;
            EthReceived = ethReceived;
            TestTokensMinted = testTokensMinted;
        }

        public DateTime Utc { get; }
        public EthAddress UsedAddress { get; }
        public Transaction<Ether>? EthReceived { get; }
        public Transaction<TestToken>? TestTokensMinted { get; }
    }
}
