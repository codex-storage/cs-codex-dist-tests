using Discord;
using Discord.WebSocket;

namespace BiblioTech.Commands
{
    public class SubCommandOption : CommandOption
    {
        private readonly CommandOption[] options;

        public SubCommandOption(string name, string description, params CommandOption[] options)
            : base(name, description, type: ApplicationCommandOptionType.SubCommand, isRequired: false)
        {
            this.options = options;
        }

        public override SlashCommandOptionBuilder Build()
        {
            var builder = base.Build();
            foreach (var option in options)
            {
                builder.AddOption(option.Build());
            }
            return builder;
        }
    }

    public class AdminCommand : BaseCommand
    {
        public override string Name => "admin";
        public override string StartingMessage => "...";
        public override string Description => "Admins only.";

        private readonly SubCommandOption aaa = new SubCommandOption("aaa", "does AAA", new EthAddressOption());
        private readonly SubCommandOption bbb = new SubCommandOption("bbb", "does BBB", new UserOption("a user", true));

        public override CommandOption[] Options => new CommandOption[]
        {
            aaa, bbb
        };

        protected override Task Invoke(SocketSlashCommand command)
        {
            return Task.CompletedTask;
        }
    }
}
