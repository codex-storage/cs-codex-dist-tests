using CodexContractsPlugin;
using GethPlugin;
using Newtonsoft.Json;

namespace BiblioTech
{
    public class UserRepo
    {
        private readonly object repoLock = new object();

        public void AssociateUserWithAddress(ulong discordId, EthAddress address)
        {
            lock (repoLock)
            {
                SetUserAddress(discordId, address);
            }
        }

        public void ClearUserAssociatedAddress(ulong discordId)
        {
            lock (repoLock)
            {
                SetUserAddress(discordId, null);
            }
        }

        public void AddMintEventForUser(ulong discordId, EthAddress usedAddress, Ether eth, TestToken tokens)
        {
            lock (repoLock)
            {
                var user = GetOrCreate(discordId);
                user.MintEvents.Add(new UserMintEvent(DateTime.UtcNow, usedAddress, eth, tokens));
                SaveUser(user);
            }
        }

        public EthAddress? GetCurrentAddressForUser(ulong discordId)
        {
            lock (repoLock)
            {
                return GetOrCreate(discordId).CurrentAddress;
            }
        }

        private void SetUserAddress(ulong discordId, EthAddress? address)
        {
            var user = GetOrCreate(discordId);
            user.CurrentAddress = address;
            user.AssociateEvents.Add(new UserAssociateAddressEvent(DateTime.UtcNow, address));
            SaveUser(user);
        }

        private User GetOrCreate(ulong discordId)
        {
            var filename = GetFilename(discordId);
            if (!File.Exists(filename))
            {
                return CreateAndSaveNewUser(discordId);
            }
            return JsonConvert.DeserializeObject<User>(File.ReadAllText(filename))!;
        }

        private User CreateAndSaveNewUser(ulong discordId)
        {
            var newUser = new User(discordId, DateTime.UtcNow, null, new List<UserAssociateAddressEvent>(), new List<UserMintEvent>());
            SaveUser(newUser);
            return newUser;
        }

        private void SaveUser(User user)
        {
            var filename = GetFilename(user.DiscordId);
            if (File.Exists(filename)) File.Delete(filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(user));
        }

        private static string GetFilename(ulong discordId)
        {
            return Path.Combine(Program.Config.UserDataPath, discordId.ToString() + ".json");
        }
    }

    public class User
    {
        public User(ulong discordId, DateTime createdUtc, EthAddress? currentAddress, List<UserAssociateAddressEvent> associateEvents, List<UserMintEvent> mintEvents)
        {
            DiscordId = discordId;
            CreatedUtc = createdUtc;
            CurrentAddress = currentAddress;
            AssociateEvents = associateEvents;
            MintEvents = mintEvents;
        }

        public ulong DiscordId { get; }
        public DateTime CreatedUtc { get; }
        public EthAddress? CurrentAddress { get; set; }
        public List<UserAssociateAddressEvent> AssociateEvents { get; }
        public List<UserMintEvent> MintEvents { get; }
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
        public UserMintEvent(DateTime utc, EthAddress usedAddress, Ether ethReceived, TestToken testTokensMinted)
        {
            Utc = utc;
            UsedAddress = usedAddress;
            EthReceived = ethReceived;
            TestTokensMinted = testTokensMinted;
        }

        public DateTime Utc { get; }
        public EthAddress UsedAddress { get; }
        public Ether EthReceived { get; }
        public TestToken TestTokensMinted { get; }
    }
}
