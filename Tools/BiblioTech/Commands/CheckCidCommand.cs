using BiblioTech.Options;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace BiblioTech.Commands
{
    public class CheckCidCommand : BaseCommand
    {
        private readonly StringOption cidOption = new StringOption(
            name: "cid",
            description: "Codex Content-Identifier",
            isRequired: true);
        private readonly CodexCidChecker checker;
        private readonly CidStorage cidStorage;
        private const ulong ALTRUISTIC_ROLE_ID = 1286134120379977860;

        public CheckCidCommand(CodexCidChecker checker)
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

            var response = await checker.PerformCheck(cid);
            await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' for cid '{cid}'. Lookup-success: {response.Success}. Message: '{response.Message}' Error: '{response.Error}'");

            if (response.Success)
            {
                if (cidStorage.IsCidUsed(cid))
                {
                    await context.Followup($"{response.Message}\n\nThis CID has already been used by another user. No role will be granted.");
                    return;
                }

                if (cidStorage.AddCid(cid, user.Id))
                {
                    var guildUser = context.Command.User as IGuildUser;
                    if (guildUser != null)
                    {
                        try
                        {
                            var role = context.Command.Guild.GetRole(ALTRUISTIC_ROLE_ID);
                            if (role != null)
                            {
                                await guildUser.AddRoleAsync(role);
                                await context.Followup($"{response.Message}\n\nCongratulations! You've been granted the Altruistic Mode role for checking a valid CID!");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            await Program.AdminChecker.SendInAdminChannel($"Failed to grant Altruistic Mode role to user {Mention(user)}: {ex.Message}");
                        }
                    }
                }
            }

            await context.Followup(response.Message);
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

        public bool AddCid(string cid, ulong userId)
        {
            lock (_lock)
            {
                var existingEntries = File.ReadAllLines(filePath);
                if (existingEntries.Any(line => line.Split(',')[0] == cid))
                {
                    return false;
                }

                File.AppendAllText(filePath, $"{cid},{userId}{Environment.NewLine}");
                return true;
            }
        }

        public bool IsCidUsed(string cid)
        {
            lock (_lock)
            {
                var existingEntries = File.ReadAllLines(filePath);
                return existingEntries.Any(line => line.Split(',')[0] == cid);
            }
        }
    }
}
