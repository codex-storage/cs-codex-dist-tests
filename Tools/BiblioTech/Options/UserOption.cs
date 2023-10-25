using Discord;

namespace BiblioTech.Options
{
    public class UserOption : CommandOption
    {
        public UserOption(string description, bool isRequired)
            : base("user", description, ApplicationCommandOptionType.User, isRequired)
        {
        }

        public IUser? GetUser(CommandContext context)
        {
            var userOptionData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (userOptionData == null) return null;
            return userOptionData.Value as IUser;
        }
    }
}
