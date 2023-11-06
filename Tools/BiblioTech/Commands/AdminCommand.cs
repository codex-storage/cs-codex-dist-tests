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
        private readonly DeployListCommand deployListCommand = new DeployListCommand();
        private readonly DeployUploadCommand deployUploadCommand = new DeployUploadCommand();
        private readonly DeployRemoveCommand deployRemoveCommand = new DeployRemoveCommand();
        private readonly WhoIsCommand whoIsCommand = new WhoIsCommand();
        private readonly NetInfoCommand netInfoCommand;
        private readonly DebugPeerCommand debugPeerCommand;

        public AdminCommand(CoreInterface ci)
        {
            netInfoCommand = new NetInfoCommand(ci);
            debugPeerCommand = new DebugPeerCommand(ci);
        }

        public override string Name => "admin";
        public override string StartingMessage => "...";
        public override string Description => "Admins only.";

        public override CommandOption[] Options => new CommandOption[]
        {
            clearCommand,
            reportCommand,
            deployListCommand,
            deployUploadCommand,
            deployRemoveCommand,
            whoIsCommand,
            netInfoCommand,
            debugPeerCommand
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
            await deployListCommand.CommandHandler(context);
            await deployUploadCommand.CommandHandler(context);
            await deployRemoveCommand.CommandHandler(context);
            await whoIsCommand.CommandHandler(context);
            await netInfoCommand.CommandHandler(context);
            await debugPeerCommand.CommandHandler(context);
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

                var report = string.Join(Environment.NewLine, Program.UserRepo.GetInteractionReport(user));
                if (report.Length > 1900)
                {
                    var filename = $"user-{user.Username}.log";
                    await context.FollowupWithAttachement(filename, report);
                }
                else
                {
                    await context.Followup(report);
                }
            }
        }

        public class DeployListCommand : SubCommandOption
        {
            public DeployListCommand()
                : base("list", "Lists current deployments.")
            {
            }

            protected override async Task onSubCommand(CommandContext context)
            {
                var deployments = Program.DeploymentFilesMonitor.GetDeployments();

                //todo shows old deployments

                if (!deployments.Any())
                {
                    await context.Followup("No deployments available.");
                    return;
                }

                var nl = Environment.NewLine;
                await context.Followup($"Deployments:{nl}{string.Join(nl, deployments.Select(FormatDeployment))}");
            }

            private string FormatDeployment(CodexDeployment deployment)
            {
                var m = deployment.Metadata;
                return $"'{m.Name}' ({m.StartUtc.ToString("o")})";
            }
        }

        public class DeployUploadCommand : SubCommandOption
        {
            private readonly FileAttachementOption fileOption = new FileAttachementOption(
                name: "json",
                description: "Codex-deployment json to add.",
                isRequired: true);

            public DeployUploadCommand()
                : base("add", "Upload a new deployment JSON file.")
            {
            }

            public override CommandOption[] Options => new[] { fileOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var file = await fileOption.Parse(context);
                if (file == null) return;

                var result = await Program.DeploymentFilesMonitor.DownloadDeployment(file);
                if (result)
                {
                    await context.Followup("Success!");
                }
                else
                {
                    await context.Followup("That didn't work.");
                }
            }
        }

        public class DeployRemoveCommand : SubCommandOption
        {
            private readonly StringOption stringOption = new StringOption(
                name: "name",
                description: "Name of deployment to remove.",
                isRequired: true);

            public DeployRemoveCommand()
                : base("remove", "Removes a deployment file.")
            {
            }

            public override CommandOption[] Options => new[] { stringOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var str = await stringOption.Parse(context);
                if (string.IsNullOrEmpty(str)) return;

                var result = Program.DeploymentFilesMonitor.DeleteDeployment(str);
                if (result)
                {
                    await context.Followup("Success!");
                }
                else
                {
                    await context.Followup("That didn't work.");
                }
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

        public abstract class AdminDeploymentCommand : SubCommandOption
        {
            private readonly CoreInterface ci;

            public AdminDeploymentCommand(CoreInterface ci, string name, string description)
                : base(name, description)
            {
                this.ci = ci;
            }

            protected async Task OnDeployment(CommandContext context, Func<ICodexNodeGroup, string, Task> action)
            {
                var deployment = Program.DeploymentFilesMonitor.GetDeployments().SingleOrDefault();
                if (deployment == null)
                {
                    await context.Followup("No deployment found.");
                    return;
                }

                try
                {
                    var group = ci.WrapCodexContainers(deployment.CodexInstances.Select(i => i.Containers).ToArray());
                    await action(group, deployment.Metadata.Name);
                }
                catch (Exception ex)
                {
                    await context.Followup("Failed to wrap nodes with exception: " + ex);
                }
            }
        }

        public class NetInfoCommand : AdminDeploymentCommand
        {
            public NetInfoCommand(CoreInterface ci)
                : base(ci, name: "netinfo",
                      description: "Fetches info endpoints of codex nodes.")
            {
            }

            protected override async Task onSubCommand(CommandContext context)
            {
                await OnDeployment(context, async (group, name) =>
                {
                    var nl = Environment.NewLine;
                    var content = new List<string>
                    {
                        $"{DateTime.UtcNow.ToString("o")} - {group.Count()} Codex nodes."
                    };

                    foreach (var node in group)
                    {
                        try
                        {
                            var info = node.GetDebugInfo();
                            var json = JsonConvert.SerializeObject(info, Formatting.Indented);
                            var jsonInsert = $"{nl}```{nl}{json}{nl}```{nl}";
                            content.Add($"Node '{node.GetName()}' responded with {jsonInsert}");
                        }
                        catch (Exception ex)
                        {
                            content.Add($"Node '{node.GetName()}' failed to respond with exception: " + ex);
                        }
                    }

                    var filename = $"netinfo-{NoWhitespaces(name)}.log";
                    await context.FollowupWithAttachement(filename, string.Join(nl, content.ToArray()));
                });
            }
        }

        public class DebugPeerCommand : AdminDeploymentCommand
        {
            private readonly StringOption peerIdOption = new StringOption("peerid", "id of peer to try and reach.", true);

            public DebugPeerCommand(CoreInterface ci)
                : base(ci, name: "debugpeer",
                      description: "Calls debug/peer on each codex node.")
            {
            }

            public override CommandOption[] Options => new[] { peerIdOption };

            protected override async Task onSubCommand(CommandContext context)
            {
                var peerId = await peerIdOption.Parse(context);
                if (string.IsNullOrEmpty(peerId)) return;

                await OnDeployment(context, async (group, name) =>
                {
                    await context.Followup($"Calling debug/peer for '{peerId}' on {group.Count()} Codex nodes.");
                    foreach (var node in group)
                    {
                        try
                        {
                            var info = node.GetDebugPeer(peerId);
                            var nl = Environment.NewLine;
                            var json = JsonConvert.SerializeObject(info, Formatting.Indented);
                            var jsonInsert = $"{nl}```{nl}{json}{nl}```{nl}";
                            await context.Followup($"Node '{node.GetName()}' responded with {jsonInsert}");
                        }
                        catch (Exception ex)
                        {
                            await context.Followup($"Node '{node.GetName()}' failed to respond with exception: " + ex);
                        }
                    }
                });
            }
        }

        private static string NoWhitespaces(string s)
        {
            return s.Replace(" ", "-");
        }
    }
}
