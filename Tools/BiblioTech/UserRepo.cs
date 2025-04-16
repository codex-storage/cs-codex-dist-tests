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
        private readonly Dictionary<ulong, UserData> cache = new Dictionary<ulong, UserData>();

        public SetAddressResponse AssociateUserWithAddress(IUser user, EthAddress address)
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

        public void SetUserNotificationPreference(IUser user, bool enableNotifications)
        {
            lock (repoLock)
            {
                SetUserNotification(user, enableNotifications);
            }
        }

        public UserData[] GetAllUserData()
        {
            if (cache.Count == 0) LoadAllUserData();
            return cache.Values.ToArray();  
        }

        public UserData? GetUserById(ulong id)
        {
            if (cache.Count == 0) LoadAllUserData();
            if (cache.ContainsKey(id)) return cache[id];
            return null;
        }

        public void AddMintEventForUser(IUser user, EthAddress usedAddress, Transaction<Ether>? eth, Transaction<TestToken>? tokens)
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
                        if (me.EthReceived != null)
                        {
                            result.Add($"{me.Utc.ToString("o")} - Sent {me.EthReceived.TokenAmount} to {me.UsedAddress}. ({me.EthReceived.TransactionHash})");
                        }
                        if (me.TestTokensMinted != null)
                        {
                            result.Add($"{me.Utc.ToString("o")} - Minted {me.TestTokensMinted.TokenAmount} to {me.UsedAddress}. ({me.TestTokensMinted.TransactionHash})");
                        }
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

        public UserData? GetUserDataForAddress(EthAddress? address)
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

        private SetAddressResponse SetUserAddress(IUser user, EthAddress? address)
        {
            if (GetUserDataForAddress(address) != null)
            {
                return SetAddressResponse.AddressAlreadyInUse;
            }

            var userData = GetOrCreate(user);
            if (userData == null) return SetAddressResponse.CreateUserFailed;
            userData.CurrentAddress = address;
            userData.AssociateEvents.Add(new UserAssociateAddressEvent(DateTime.UtcNow, address));
            SaveUserData(userData);
            return SetAddressResponse.OK;
        }

        private void SetUserNotification(IUser user, bool notifyEnabled)
        {
            var userData = GetUserData(user);
            if (userData == null) return;
            userData.NotificationsEnabled = notifyEnabled;
            SaveUserData(userData);
        }

        private UserData? GetUserData(IUser user)
        {
            if (cache.ContainsKey(user.Id))
            {
                return cache[user.Id];
            }

            var filename = GetFilename(user);
            if (!File.Exists(filename))
            {
                return null;
            }
            var userData = JsonConvert.DeserializeObject<UserData>(File.ReadAllText(filename))!;
            cache.Add(userData.DiscordId, userData);
            return userData;
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
            var newUser = new UserData(user.Id, user.GlobalName, DateTime.UtcNow, null, new List<UserAssociateAddressEvent>(), new List<UserMintEvent>(), true);
            SaveUserData(newUser);
            return newUser;
        }

        private void SaveUserData(UserData userData)
        {
            var filename = GetFilename(userData);
            if (File.Exists(filename)) File.Delete(filename);
            File.WriteAllText(filename, JsonConvert.SerializeObject(userData));

            if (cache.ContainsKey(userData.DiscordId))
            {
                cache[userData.DiscordId] = userData;
            }
            else
            {
                cache.Add(userData.DiscordId, userData);
            }
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

        private void LoadAllUserData()
        {
            try
            {
                var files = Directory.GetFiles(Program.Config.UserDataPath);
                foreach (var file in files)
                {
                    try
                    {
                        var userData = JsonConvert.DeserializeObject<UserData>(File.ReadAllText(file))!;
                        if (userData != null && userData.DiscordId > 0)
                        {
                            cache.Add(userData.DiscordId, userData);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Program.Log.Error("Exception while trying to load all user data: " + ex);
            }
        }
    }

    public enum SetAddressResponse
    {
        OK,
        AddressAlreadyInUse,
        CreateUserFailed
    }
}
