using BiblioTech.Options;
using Discord;
using Discord.WebSocket;
using Org.BouncyCastle.Utilities;

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

        public async Task SendInAdminChannel(string msg)
        {
            await SendInAdminChannel(msg.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }

        public async Task SendInAdminChannel(string[] lines)
        {
            var chunker = new LineChunker(lines);
            var chunks = chunker.GetChunks();
            if (!chunks.Any()) return;

            foreach (var chunk in chunks)
            {
                await adminChannel.SendMessageAsync(string.Join(Environment.NewLine, chunk));
            }
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
