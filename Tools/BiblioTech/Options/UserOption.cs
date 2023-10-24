using Discord;

namespace BiblioTech.Options
{
    public class UserOption : CommandOption
    {
        public UserOption(string description, bool isRequired)
            : base("user", description, ApplicationCommandOptionType.User, isRequired)
        {
        }

        public ulong? GetOptionUserId(CommandContext context)
        {
            var userOptionData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (userOptionData == null) return null;
            var user = userOptionData.Value as IUser;
            if (user == null) return null;
            return user.Id;
        }
    }
}
