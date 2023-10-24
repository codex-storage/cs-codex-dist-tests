using Discord;

namespace BiblioTech.Options
{
    public class StringOption : CommandOption
    {
        public StringOption(string name, string description, bool isRequired)
            : base(name, description, type: ApplicationCommandOptionType.String, isRequired)
        {
        }

        public async Task<string?> Parse(CommandContext context)
        {
            var strData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (strData == null)
            {
                await context.Command.FollowupAsync("String option not received.");
                return null;
            }
            return strData.Value as string;
        }
    }
}
