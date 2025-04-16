using BiblioTech.Options;
using BiblioTech.Rewards;

namespace BiblioTech.Commands
{
    public class AdminCommand : BaseCommand
    {
        private readonly ClearUserAssociationCommand clearCommand = new ClearUserAssociationCommand();
        private readonly ReportCommand reportCommand = new ReportCommand();
        private readonly WhoIsCommand whoIsCommand = new WhoIsCommand();
        private readonly LogReplaceCommand logReplaceCommand;

        public AdminCommand(CustomReplacement replacement)
        {
            logReplaceCommand = new LogReplaceCommand(replacement);
        }

        public override string Name => "admin";
        public override string StartingMessage => "...";
        public override string Description => "Admins only.";

        public override CommandOption[] Options => new CommandOption[]
        {
            clearCommand,
            reportCommand,
            whoIsCommand,
            logReplaceCommand
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
            await logReplaceCommand.CommandHandler(context);
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

        public class LogReplaceCommand : SubCommandOption
        {
            private readonly CustomReplacement replacement;
            private readonly StringOption fromOption = new StringOption("from", "string to replace", true);
            private readonly StringOption toOption = new StringOption("to", "string to replace with", false);

            public LogReplaceCommand(CustomReplacement replacement)
                : base(name: "logreplace",
               description: "Replaces all 'from' with 'to' in ChainEvents.")
            {
                this.replacement = replacement;
            }

            public override CommandOption[] Options => new[] { fromOption, toOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var from = await fromOption.Parse(context);
                var to = await toOption.Parse(context);

                if (string.IsNullOrEmpty(from))
                {
                    await context.Followup("'from' not received");
                    return;
                }

                if (from.Length < 5)
                {
                    await context.Followup("'from' must be length 5 or greater.");
                    return;
                }
                
                if (string.IsNullOrEmpty(to))
                {
                    replacement.Remove(from);
                    await context.Followup($"Replace for '{from}' removed.");
                }
                else
                {
                    if (to.Length < 5)
                    {
                        await context.Followup("'to' must be length 5 or greater.");
                        return;
                    }

                    replacement.Add(from, to);
                    await context.Followup($"Replace added '{from}' -->> '{to}'.");
                }
            }
        }
    }
}
