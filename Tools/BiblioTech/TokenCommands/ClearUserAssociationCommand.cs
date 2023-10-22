using Discord.WebSocket;

namespace BiblioTech.TokenCommands
{
    public class ClearUserAssociationCommand : BaseCommand
    {
        private readonly UserOption user = new UserOption(
            description: "User to clear Eth address for.",
            isRequired: true);

        public override string Name => "clear";
        public override string StartingMessage => "Hold on...";
        public override string Description => "Admin only. Clears current Eth address for a user, allowing them to set a new one.";

        protected override async Task Invoke(SocketSlashCommand command)
        {
            if (!IsSenderAdmin(command))
            {
                await command.FollowupAsync("You're not an admin.");
                return;
            }

            var userId = user.GetOptionUserId(command);
            if (userId == null)
            {
                await command.FollowupAsync("Failed to get user ID");
                return;
            }

            Program.UserRepo.ClearUserAssociatedAddress(userId.Value);
            await command.FollowupAsync("Done."); ;
        }
    }
}
