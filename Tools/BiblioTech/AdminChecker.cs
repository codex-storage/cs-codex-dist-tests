using Discord.WebSocket;

namespace BiblioTech
{
    public class AdminChecker
    {
        private SocketGuild guild = null!;
        private ulong[] adminIds = Array.Empty<ulong>();
        private DateTime lastUpdate = DateTime.MinValue;

        public void SetGuild(SocketGuild guild)
        {
            this.guild = guild;
        }

        public bool IsUserAdmin(ulong userId)
        {
            if (ShouldUpdate()) UpdateAdminIds();

            return adminIds.Contains(userId);
        }

        public bool IsAdminChannel(ISocketMessageChannel channel)
        {
            return channel.Name == Program.Config.AdminChannelName;
        }

        private bool ShouldUpdate()
        {
            return !adminIds.Any() || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }

        private void UpdateAdminIds()
        {
            lastUpdate = DateTime.UtcNow;
            var adminRole = guild.Roles.Single(r => r.Name == Program.Config.AdminRoleName);
            adminIds = adminRole.Members.Select(m => m.Id).ToArray();
        }
    }
}
