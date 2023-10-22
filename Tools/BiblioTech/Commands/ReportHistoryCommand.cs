using Discord.WebSocket;

namespace BiblioTech.Commands
{
    public class ReportHistoryCommand : BaseCommand
    {
        private readonly UserOption user = new UserOption(
            description: "User to report history for.",
            isRequired: true);

        public override string Name => "report";
        public override string StartingMessage => "Getting that data...";
        public override string Description => "Admin only. Reports bot-interaction history for a user.";
        public override CommandOption[] Options => new[] { user };

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

            var report = Program.UserRepo.GetInteractionReport(userId.Value);
            await command.FollowupAsync(string.Join(Environment.NewLine, report));
        }
    }
}
