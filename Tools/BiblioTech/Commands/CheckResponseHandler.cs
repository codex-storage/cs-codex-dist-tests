using BiblioTech.CodexChecking;
using BiblioTech.Options;
using Discord;

namespace BiblioTech.Commands
{
    public class CheckResponseHandler : ICheckResponseHandler
    {
        private CommandContext context;
        private readonly IUser user;

        public CheckResponseHandler(CommandContext context, IUser user)
        {
            this.context = context;
            this.user = user;
        }

        public async Task CheckNotStarted()
        {
            await context.Followup("Run this command without any arguments first, to begin the check process.");
        }

        public async Task CouldNotDownloadCid()
        {
            await context.Followup("Could not download the CID.");
        }

        public async Task GiveCidToUser(string cid)
        {
            await context.Followup("Please download this CID using your Codex node. " +
                "Then provide the content of the downloaded file as argument to this command. " +
                $"`{cid}`");
        }

        public async Task GiveDataFileToUser(string fileContent)
        {
            await context.SendFile(fileContent, "Please download the attached file. Upload it to your Codex node, " +
                "then provide the CID as argument to this command.");
        }

        public async Task GiveRoleReward()
        {
            try
            {
                await Program.RoleDriver.GiveAltruisticRole(user);
                await context.Followup($"Congratulations! You've been granted the Altruistic Mode role!");
            }
            catch (Exception ex)
            {
                await Program.AdminChecker.SendInAdminChannel($"Failed to grant Altruistic Mode role to user <@{user.Id}>: {ex.Message}");
            }
        }

        public async Task InvalidData()
        {
            await context.Followup("The received data didn't match. Check has failed.");
        }

        public async Task NowCompleted()
        {
            await context.Followup("Successfully completed the check!");
        }
    }
}
