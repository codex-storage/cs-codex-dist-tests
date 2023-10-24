using BiblioTech.Options;
using CodexPlugin;

namespace BiblioTech.Commands
{
    public class AdminCommand : BaseCommand
    {
        private readonly ClearUserAssociationCommand clearCommand;
        private readonly ReportCommand reportCommand;
        private readonly DeployListCommand deployListCommand;
        private readonly DeployUploadCommand deployUploadCommand;
        private readonly DeployRemoveCommand deployRemoveCommand;

        public override string Name => "admin";
        public override string StartingMessage => "...";
        public override string Description => "Admins only.";

        public AdminCommand(DeploymentsFilesMonitor monitor)
        {
            clearCommand = new ClearUserAssociationCommand();
            reportCommand = new ReportCommand();
            deployListCommand = new DeployListCommand(monitor);
            deployUploadCommand = new DeployUploadCommand(monitor);
            deployRemoveCommand = new DeployRemoveCommand(monitor);
        }

        public override CommandOption[] Options => new CommandOption[]
        {
            clearCommand,
            reportCommand,
            deployListCommand,
            deployUploadCommand,
            deployRemoveCommand
        };

        protected override async Task Invoke(CommandContext context)
        {
            if (!IsSenderAdmin(context.Command))
            {
                await context.Command.FollowupAsync("You're not an admin.");
                return;
            }

            await clearCommand.CommandHandler(context);
            await reportCommand.CommandHandler(context);
            await deployListCommand.CommandHandler(context);
            await deployUploadCommand.CommandHandler(context);
            await deployRemoveCommand.CommandHandler(context);
        }

        public class ClearUserAssociationCommand : SubCommandOption
        {
            private readonly UserOption user = new UserOption("User to clear Eth address for.", true);

            public ClearUserAssociationCommand()
                : base("clear", "Admin only. Clears current Eth address for a user, allowing them to set a new one.")
            {
            }

            public override CommandOption[] Options => new[] { user };

            protected override async Task onSubCommand(CommandContext context)
            {
                var userId = user.GetOptionUserId(context);
                if (userId == null)
                {
                    await context.Command.FollowupAsync("Failed to get user ID");
                    return;
                }

                Program.UserRepo.ClearUserAssociatedAddress(userId.Value);
                await context.Command.FollowupAsync("Done.");
            }
        }

        public class ReportCommand : SubCommandOption
        {
            private readonly UserOption user = new UserOption(
                description: "User to report history for.",
                isRequired: true);

            public ReportCommand()
                : base("report", "Admin only. Reports bot-interaction history for a user.")
            {
            }

            public override CommandOption[] Options => new[] { user };

            protected override async Task onSubCommand(CommandContext context)
            {
                var userId = user.GetOptionUserId(context);
                if (userId == null)
                {
                    await context.Command.FollowupAsync("Failed to get user ID");
                    return;
                }

                var report = Program.UserRepo.GetInteractionReport(userId.Value);
                await context.Command.FollowupAsync(string.Join(Environment.NewLine, report));
            }
        }

        public class DeployListCommand : SubCommandOption
        {
            private readonly DeploymentsFilesMonitor monitor;

            public DeployListCommand(DeploymentsFilesMonitor monitor)
                : base("list", "Lists current deployments.")
            {
                this.monitor = monitor;
            }

            protected override async Task onSubCommand(CommandContext context)
            {
                var deployments = monitor.GetDeployments();

                if (!deployments.Any())
                {
                    await context.Command.FollowupAsync("No deployments available.");
                    return;
                }

                await context.Command.FollowupAsync($"Deployments: {string.Join(", ", deployments.Select(FormatDeployment))}");
            }

            private string FormatDeployment(CodexDeployment deployment)
            {
                var m = deployment.Metadata;
                return $"'{m.Name}' ({m.StartUtc.ToString("o")})";
            }
        }

        public class DeployUploadCommand : SubCommandOption
        {
            private readonly DeploymentsFilesMonitor monitor;
            private readonly FileAttachementOption fileOption = new FileAttachementOption(
                name: "json",
                description: "Codex-deployment json to add.",
                isRequired: true);

            public DeployUploadCommand(DeploymentsFilesMonitor monitor)
                : base("add", "Upload a new deployment JSON file.")
            {
                this.monitor = monitor;
            }

            public override CommandOption[] Options => new[] { fileOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var file = await fileOption.Parse(context);
                if (file == null) return;

                var result = await monitor.DownloadDeployment(file);
                if (result)
                {
                    await context.Command.FollowupAsync("Success!");
                }
                else
                {
                    await context.Command.FollowupAsync("That didn't work.");
                }
            }
        }

        public class DeployRemoveCommand : SubCommandOption
        {
            private readonly DeploymentsFilesMonitor monitor;
            private readonly StringOption stringOption = new StringOption(
                name: "deployment name",
                description: "Name of deployment to remove.",
                isRequired: true);

            public DeployRemoveCommand(DeploymentsFilesMonitor monitor)
                : base("remove", "Removes a deployment file.")
            {
                this.monitor = monitor;
            }

            public override CommandOption[] Options => new[] { stringOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var str = await stringOption.Parse(context);
                if (string.IsNullOrEmpty(str)) return;

                var result = monitor.DeleteDeployment(str);
                if (result)
                {
                    await context.Command.FollowupAsync("Success!");
                }
                else
                {
                    await context.Command.FollowupAsync("That didn't work.");
                }
            }
        }
    }
}
