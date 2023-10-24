using Discord;

namespace BiblioTech.Options
{
    public abstract class SubCommandOption : CommandOption
    {
        public SubCommandOption(string name, string description)
            : base(name, description, type: ApplicationCommandOptionType.SubCommand, isRequired: false)
        {
        }

        public override SlashCommandOptionBuilder Build()
        {
            var builder = base.Build();
            foreach (var option in Options)
            {
                builder.AddOption(option.Build());
            }
            return builder;
        }

        public async Task CommandHandler(CommandContext context)
        {
            var mine = context.Options.SingleOrDefault(o => o.Name == Name);
            if (mine == null) return;

            await onSubCommand(new CommandContext(context.Command, mine.Options));
        }

        public virtual CommandOption[] Options
        {
            get
            {
                return Array.Empty<CommandOption>();
            }
        }

        protected abstract Task onSubCommand(CommandContext context);
    }
}
