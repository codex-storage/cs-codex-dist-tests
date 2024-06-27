using Discord;
using Discord.WebSocket;

namespace BiblioTech
{
    public class AdminChecker
    {
        private SocketGuild guild = null!;
        private ulong[] adminIds = Array.Empty<ulong>();
        private DateTime lastUpdate = DateTime.MinValue;
        private ISocketMessageChannel adminChannel = null!;

        public void SetGuild(SocketGuild guild)
        {
            this.guild = guild;
        }

        public bool IsUserAdmin(ulong userId)
        {
            if (ShouldUpdate()) UpdateAdminIds();

            return adminIds.Contains(userId);
        }

        public bool IsAdminChannel(IChannel channel)
        {
            return channel.Id == Program.Config.AdminChannelId;
        }

        public ISocketMessageChannel GetAdminChannel()
        {
            return adminChannel;
        }

        public void SetAdminChannel(ISocketMessageChannel adminChannel)
        {
            this.adminChannel = adminChannel;
        }

        private bool ShouldUpdate()
        {
            return !adminIds.Any() || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }

        private void UpdateAdminIds()
        {
            lastUpdate = DateTime.UtcNow;
            var adminRole = guild.Roles.Single(r => r.Id == Program.Config.AdminRoleId);
            adminIds = adminRole.Members.Select(m => m.Id).ToArray();
        }
    }
}
