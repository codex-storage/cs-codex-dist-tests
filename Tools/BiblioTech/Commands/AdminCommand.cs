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
        private readonly AddSprCommand addSprCommand;
        private readonly ClearSprsCommand clearSprsCommand;
        private readonly GetSprCommand getSprCommand;

        public AdminCommand(CoreInterface ci, SprCommand sprCommand)
        {
            netInfoCommand = new NetInfoCommand(ci);
            debugPeerCommand = new DebugPeerCommand(ci);
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
            deployListCommand,
            deployUploadCommand,
            deployRemoveCommand,
            whoIsCommand,
            netInfoCommand,
            debugPeerCommand,
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
            await deployListCommand.CommandHandler(context);
            await deployUploadCommand.CommandHandler(context);
            await deployRemoveCommand.CommandHandler(context);
            await whoIsCommand.CommandHandler(context);
            await netInfoCommand.CommandHandler(context);
            await debugPeerCommand.CommandHandler(context);
            await addSprCommand.CommandHandler(context);
            await clearSprsCommand.CommandHandler(context);
            await deployUploadCommand.CommandHandler(context);
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

        public class DeployListCommand : SubCommandOption
        {
            public DeployListCommand()
                : base("list", "Lists current deployments.")
            {
            }

            protected override async Task onSubCommand(CommandContext context)
            {
                var deployments = Program.DeploymentFilesMonitor.GetDeployments();
                if (!deployments.Any())
                {
                    await context.Followup("No deployments available.");
                    return;
                }

                var nl = Environment.NewLine;
                await context.Followup(deployments.Select(FormatDeployment).ToArray());
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

            protected async Task<T?> OnDeployment<T>(CommandContext context, Func<ICodexNodeGroup, string, T> action)
            {
                var deployment = Program.DeploymentFilesMonitor.GetDeployments().SingleOrDefault();
                if (deployment == null)
                {
                    await context.Followup("No deployment found.");
                    return default;
                }
                if (deployment.CodexInstances == null || !deployment.CodexInstances.Any())
                {
                    await context.Followup("No codex instances were deployed.");
                    return default;
                }

                try
                {
                    var group = ci.WrapCodexContainers(deployment.CodexInstances.Select(i => i.Containers).ToArray());
                    var result = action(group, deployment.Metadata.Name);
                    return result;
                }
                catch (Exception ex)
                {
                    var message = new[]
                    {
                        "Failed to wrap nodes with exception: "
                    };
                    var exceptionMessage = ex.ToString().Split(Environment.NewLine);

                    await context.Followup(message.Concat(exceptionMessage).ToArray());
                    return default;
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
                var report = await OnDeployment(context, CreateNetInfoReport);
                if (report != null && report.Any())
                {
                    await context.Followup(report);
                }
            }

            private string[] CreateNetInfoReport(ICodexNodeGroup group, string name)
            {
                var content = new List<string>
                {
                    $"{DateTime.UtcNow.ToString("o")} - {group.Count()} Codex nodes.",
                    $"Deployment name: '{name}'."
                };

                foreach (var node in group)
                {
                    try
                    {
                        var info = node.GetDebugInfo();
                        var json = JsonConvert.SerializeObject(info, Formatting.Indented);
                        var jsonLines = json.Split(Environment.NewLine);
                        content.Add($"Node '{node.GetName()}' responded with:");
                        content.Add("---");
                        content.AddRange(jsonLines);
                        content.Add("---");
                    }
                    catch (Exception ex)
                    {
                        content.Add($"Node '{node.GetName()}' failed to respond with exception: " + ex);
                    }
                }

                return content.ToArray();
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

                var report = await OnDeployment(context, (group, name) => CreateDebugPeerReport(group, name, peerId));
                if (report != null && report.Any())
                {
                    await context.Followup(report);
                }
            }

            private string[] CreateDebugPeerReport(ICodexNodeGroup group, string name, string peerId)
            {
                var content = new List<string>
                {
                    $"{DateTime.UtcNow.ToString("o")} - {group.Count()} Codex nodes.",
                    $"Deployment name: '{name}'.",
                    $"Calling debug/peer for '{peerId}'."
                };

                foreach (var node in group)
                {
                    try
                    {
                        var info = node.GetDebugPeer(peerId);
                        var json = JsonConvert.SerializeObject(info, Formatting.Indented);
                        var jsonLines = json.Split(Environment.NewLine);
                        content.Add($"Node '{node.GetName()}' responded with:");
                        content.Add("---");
                        content.AddRange(jsonLines);
                        content.Add("---");
                    }
                    catch (Exception ex)
                    {
                        content.Add($"Node '{node.GetName()}' failed to respond with exception: " + ex);
                    }
                }

                return content.ToArray();
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
