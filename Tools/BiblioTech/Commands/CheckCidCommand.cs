using BiblioTech.Options;
using Discord;
using Discord.WebSocket;

namespace BiblioTech.Commands
{
    public class CheckCidCommand : BaseCommand
    {
        private readonly StringOption cidOption = new StringOption(
            name: "cid",
            description: "Codex Content-Identifier",
            isRequired: true);
        private readonly CodexTwoWayChecker checker;
        private readonly CidStorage cidStorage;

        public CheckCidCommand(CodexTwoWayChecker checker)
        {
            this.checker = checker;
            this.cidStorage = new CidStorage(Path.Combine(Program.Config.DataPath, "valid_cids.txt"));
        }

        public override string Name => "check";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Checks if content is available in the testnet.";
        public override CommandOption[] Options => new[] { cidOption };

        protected override async Task Invoke(CommandContext context)
        {
            var user = context.Command.User;
            var cid = await cidOption.Parse(context);
            if (string.IsNullOrEmpty(cid))
            {
                await context.Followup("Option 'cid' was not received.");
                return;
            }

            try
            {
                await PerformCheck(context, user, cid);
            }
            catch (Exception ex)
            {
                await RespondeWithError(context, ex);
            }
        }

        private async Task PerformCheck(CommandContext context, SocketUser user, string cid)
        {
            var response = checker.PerformCheck(cid);
            if (response.Success)
            {
                await CheckAltruisticRole(context, user, cid, response.Message);
                return;
            }

            await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' for cid '{cid}'. Lookup-success: {response.Success}. Message: '{response.Message}'");
            await context.Followup(response.Message);
        }

        private async Task RespondeWithError(CommandContext context, Exception ex)
        {
            await Program.AdminChecker.SendInAdminChannel("Exception during CheckCidCommand: " + ex);
            await context.Followup("I'm sorry to report something has gone wrong in an unexpected way. Error details are already posted in the admin channel.");
        }

        private async Task CheckAltruisticRole(CommandContext context, IUser user, string cid, string responseMessage)
        {
            if (cidStorage.TryAddCid(cid, user.Id))
            {
                if (await GiveAltruisticRole(context, user, responseMessage))
                {
                    return;
                }
            }
            else
            {
                await context.Followup($"{responseMessage}\n\nThis CID has already been used by another user. No role will be granted.");
                return;
            }

            await context.Followup(responseMessage);
        }

        private async Task<bool> GiveAltruisticRole(CommandContext context, IUser user, string responseMessage)
        {
            try
            {
                await Program.RoleDriver.GiveAltruisticRole(user);
                await context.Followup($"{responseMessage}\n\nCongratulations! You've been granted the Altruistic Mode role for checking a valid CID!");
                return true;
            }
            catch (Exception ex)
            {
                await Program.AdminChecker.SendInAdminChannel($"Failed to grant Altruistic Mode role to user {Mention(user)}: {ex.Message}");
                return false;
            }
        }
    }

    public class CidStorage
    {
        private readonly string filePath;
        private static readonly object _lock = new object();

        public CidStorage(string filePath)
        {
            this.filePath = filePath;
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, string.Empty);
            }
        }

        public bool TryAddCid(string cid, ulong userId)
        {
            lock (_lock)
            {
                var existingEntries = File.ReadAllLines(filePath);
                if (existingEntries.Any(line => line.Split(',')[0] == cid))
                {
                    return false;
                }

                File.AppendAllLines(filePath, new[] { $"{cid},{userId}" });
                return true;
            }
        }
    }
}
