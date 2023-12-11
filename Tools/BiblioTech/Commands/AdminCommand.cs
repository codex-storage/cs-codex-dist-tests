using BiblioTech.Options;
using CodexPlugin;
using Core;
using Newtonsoft.Json;

namespace BiblioTech.Commands
{
    public class AdminCommand : BaseCommand
    {
        private readonly ClearUserAssociationCommand clearCommand = new ClearUserAssociationCommand();
        private readonly ReportCommand reportCommand = new ReportCommand();
        private readonly WhoIsCommand whoIsCommand = new WhoIsCommand();
        private readonly AddSprCommand addSprCommand;
        private readonly ClearSprsCommand clearSprsCommand;
        private readonly GetSprCommand getSprCommand;

        public AdminCommand(SprCommand sprCommand)
        {
            addSprCommand = new AddSprCommand(sprCommand);
            clearSprsCommand = new ClearSprsCommand(sprCommand);
            getSprCommand = new GetSprCommand(sprCommand);
        }

        public override string Name => "admin";
        public override string StartingMessage => "...";
        public override string Description => "Admins only.";

        public override CommandOption[] Options => new CommandOption[]
        {
            clearCommand,
            reportCommand,
            whoIsCommand,
            addSprCommand,
            clearSprsCommand,
            getSprCommand
        };

        protected override async Task Invoke(CommandContext context)
        {
            if (!IsSenderAdmin(context.Command))
            {
                await context.Followup("You're not an admin.");
                return;
            }

            if (!IsInAdminChannel(context.Command))
            {
                await context.Followup("Please use admin commands only in the admin channel.");
                return;
            }

            await clearCommand.CommandHandler(context);
            await reportCommand.CommandHandler(context);
            await whoIsCommand.CommandHandler(context);
            await addSprCommand.CommandHandler(context);
            await clearSprsCommand.CommandHandler(context);
        }

        public class ClearUserAssociationCommand : SubCommandOption
        {
            private readonly UserOption userOption = new UserOption("User to clear Eth address for.", true);

            public ClearUserAssociationCommand()
                : base("clear", "Admin only. Clears current Eth address for a user, allowing them to set a new one.")
            {
            }

            public override CommandOption[] Options => new[] { userOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var user = userOption.GetUser(context);
                if (user == null)
                {
                    await context.Followup("Failed to get user ID");
                    return;
                }

                Program.UserRepo.ClearUserAssociatedAddress(user);
                await context.Followup("Done.");
            }
        }

        public class ReportCommand : SubCommandOption
        {
            private readonly UserOption userOption = new UserOption(
                description: "User to report history for.",
                isRequired: true);

            public ReportCommand()
                : base("report", "Admin only. Reports bot-interaction history for a user.")
            {
            }

            public override CommandOption[] Options => new[] { userOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var user = userOption.GetUser(context);
                if (user == null)
                {
                    await context.Followup("Failed to get user ID");
                    return;
                }

                var report = Program.UserRepo.GetInteractionReport(user);
                await context.Followup(report);
            }
        }

        public class WhoIsCommand : SubCommandOption
        {
            private readonly UserOption userOption = new UserOption("User", isRequired: false);
            private readonly EthAddressOption ethAddressOption = new EthAddressOption(isRequired: false);

            public WhoIsCommand()
                : base(name: "whois",
                      description: "Fetches info about a user or ethAddress in the testnet.")
            {
            }

            public override CommandOption[] Options => new CommandOption[]
            {
                userOption,
                ethAddressOption
            };

            protected override async Task onSubCommand(CommandContext context)
            {
                var user = userOption.GetUser(context);
                var ethAddr = await ethAddressOption.Parse(context);

                if (user != null)
                {
                    await context.Followup(Program.UserRepo.GetUserReport(user));
                }
                if (ethAddr != null)
                {
                    await context.Followup(Program.UserRepo.GetUserReport(ethAddr));
                }
            }
        }

        public class AddSprCommand : SubCommandOption
        {
            private readonly SprCommand sprCommand;
            private readonly StringOption stringOption = new StringOption("spr", "Codex SPR", true);

            public AddSprCommand(SprCommand sprCommand)
                : base(name: "addspr",
               description: "Adds a Codex SPR, to be given to users with '/boot'.")
            {
                this.sprCommand = sprCommand;
            }

            public override CommandOption[] Options => new[] { stringOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var spr = await stringOption.Parse(context);

                if (!string.IsNullOrEmpty(spr) )
                {
                    sprCommand.Add(spr);
                    await context.Followup("A-OK!");
                }
                else
                {
                    await context.Followup("SPR is null or empty.");
                }
            }
        }

        public class ClearSprsCommand : SubCommandOption
        {
            private readonly SprCommand sprCommand;
            private readonly StringOption stringOption = new StringOption("areyousure", "set to 'true' if you are.", true);

            public ClearSprsCommand(SprCommand sprCommand)
                : base(name: "clearsprs",
               description: "Clears all Codex SPRs in the bot. Users won't be able to use '/boot' till new ones are added.")
            {
                this.sprCommand = sprCommand;
            }

            public override CommandOption[] Options => new[] { stringOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var areyousure = await stringOption.Parse(context);

                if (areyousure != "true") return;

                sprCommand.Clear();
                await context.Followup("Cleared all SPRs.");
            }
        }

        public class GetSprCommand: SubCommandOption
        {
            private readonly SprCommand sprCommand;

            public GetSprCommand(SprCommand sprCommand)
                : base(name: "getsprs",
               description: "Shows all Codex SPRs in the bot.")
            {
                this.sprCommand = sprCommand;
            }

            protected override async Task onSubCommand(CommandContext context)
            {
                await context.Followup("SPRs: " + string.Join(", ", sprCommand.Get().Select(s => $"'{s}'")));
            }
        }
    }
}
