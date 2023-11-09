using CodexContractsPlugin;
using Discord;
using GethPlugin;
using Newtonsoft.Json;
using Utils;

namespace BiblioTech
{
    public class UserRepo
    {
        private readonly object repoLock = new object();

        public bool AssociateUserWithAddress(IUser user, EthAddress address)
        {
            lock (repoLock)
            {
                return SetUserAddress(user, address);
            }
        }

        public void ClearUserAssociatedAddress(IUser user)
        {
            lock (repoLock)
            {
                SetUserAddress(user, null);
            }
        }

        public void AddMintEventForUser(IUser user, EthAddress usedAddress, Ether eth, TestToken tokens)
        {
            lock (repoLock)
            {
                var userData = GetOrCreate(user);
                userData.MintEvents.Add(new UserMintEvent(DateTime.UtcNow, usedAddress, eth, tokens));
                SaveUserData(userData);
            }
        }

        public EthAddress? GetCurrentAddressForUser(IUser user)
        {
            lock (repoLock)
            {
                return GetOrCreate(user).CurrentAddress;
            }
        }

        public string[] GetInteractionReport(IUser user)
        {
            var result = new List<string>
            {
                $"User report create on {DateTime.UtcNow.ToString("o")}"
            };

            lock (repoLock)
            {
                var userData = GetUserData(user);
                if (userData == null)
                {
                    result.Add("User has not joined the test net.");
                }
                else
                {
                    result.Add("User joined on " + userData.CreatedUtc.ToString("o"));
                    result.Add("Current address: " + userData.CurrentAddress);
                    foreach (var ae in userData.AssociateEvents)
                    {
                        result.Add($"{ae.Utc.ToString("o")} - Address set to: {ae.NewAddress}");
                    }
                    foreach (var me in userData.MintEvents)
                    {
                        result.Add($"{me.Utc.ToString("o")} - Minted {me.EthReceived} and {me.TestTokensMinted} to {me.UsedAddress}.");
                    }
                }
            }

            return result.ToArray();
        }

        public string[] GetUserReport(IUser user)
        {
            var userData = GetUserData(user);
            if (userData == null) return new[] { "User has not joined the test net." };
            return userData.CreateOverview();
        }

        public string[] GetUserReport(EthAddress ethAddress)
        {
            var userData = GetUserDataForAddress(ethAddress);
            if (userData == null) return new[] { "No user is using this eth address." };
            return userData.CreateOverview();
        }

        private bool SetUserAddress(IUser user, EthAddress? address)
        {
            if (GetUserDataForAddress(address) != null)
            {
                return false;
            }

            var userData = GetOrCreate(user);
            userData.CurrentAddress = address;
            userData.AssociateEvents.Add(new UserAssociateAddressEvent(DateTime.UtcNow, address));
            SaveUserData(userData);
            return true;
        }

        private UserData? GetUserData(IUser user)
        {
            var filename = GetFilename(user);
            if (!File.Exists(filename))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<UserData>(File.ReadAllText(filename))!;
        }

        private UserData GetOrCreate(IUser user)
        {
            var userData = GetUserData(user);
            if (userData == null)
            {
                return CreateAndSaveNewUserData(user);
            }
            return userData;
        }

        private UserData CreateAndSaveNewUserData(IUser user)
        {
            var newUser = new UserData(user.Id, user.GlobalName, DateTime.UtcNow, null, new List<UserAssociateAddressEvent>(), new List<UserMintEvent>());
            SaveUserData(newUser);
            return newUser;
        }

        private UserData? GetUserDataForAddress(EthAddress? address)
        {
            if (address == null) return null;

            // If this becomes a performance problem, switch to in-memory cached list.
            var files = Directory.GetFiles(Program.Config.UserDataPath);
            foreach (var file in files)
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<UserData>(File.ReadAllText(file))!;
                    if (user.CurrentAddress != null &&
                        user.CurrentAddress.Address == address.Address)
                    {
                        return user;
                    }
                }
                catch { }
            }

            return null;
        }

        private void SaveUserData(UserData userData)
        {
            var filename = GetFilename(userData);
            if (File.Exists(filename)) File.Delete(filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(userData));
        }

        private static string GetFilename(IUser user)
        {
            return GetFilename(user.Id);
        }

        private static string GetFilename(UserData userData)
        {
            return GetFilename(userData.DiscordId);
        }

        private static string GetFilename(ulong discordId)
        {
            return Path.Combine(Program.Config.UserDataPath, discordId.ToString() + ".json");
        }
    }

    public class UserData
    {
        public UserData(ulong discordId, string name, DateTime createdUtc, EthAddress? currentAddress, List<UserAssociateAddressEvent> associateEvents, List<UserMintEvent> mintEvents)
        {
            DiscordId = discordId;
            Name = name;
            CreatedUtc = createdUtc;
            CurrentAddress = currentAddress;
            AssociateEvents = associateEvents;
            MintEvents = mintEvents;
        }

        public ulong DiscordId { get; }
        public string Name { get; }
        public DateTime CreatedUtc { get; }
        public EthAddress? CurrentAddress { get; set; }
        public List<UserAssociateAddressEvent> AssociateEvents { get; }
        public List<UserMintEvent> MintEvents { get; }

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
