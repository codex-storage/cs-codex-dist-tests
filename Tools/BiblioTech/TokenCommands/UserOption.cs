using Discord;
using Discord.WebSocket;

namespace BiblioTech.TokenCommands
{
    public class UserOption : CommandOption
    {
        public UserOption(string description, bool isRequired)
            : base("user", description, ApplicationCommandOptionType.User, isRequired)
        {
        }

        public ulong? GetOptionUserId(SocketSlashCommand command)
        {
            var userOptionData = command.Data.Options.SingleOrDefault(o => o.Name == Name);
            if (userOptionData == null) return null;
            var user = userOptionData.Value as IUser;
            if (user == null) return null;
            return user.Id;
        }
    }
}
