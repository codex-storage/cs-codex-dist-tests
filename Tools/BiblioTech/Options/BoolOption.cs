using Discord;

namespace BiblioTech.Options
{
    public class BoolOption : CommandOption
    {
        public BoolOption(string name, string description, bool isRequired)
            : base(name, description, type: ApplicationCommandOptionType.Boolean, isRequired)
        {
        }

        public async Task<bool?> Parse(CommandContext context)
        {
            var bData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (bData == null || !(bData.Value is bool))
            {
                await context.Followup("Bool option not received.");
                return null;
            }
            return (bool) bData.Value;
        }
    }
}
