using Discord.WebSocket;

namespace BiblioTech.TokenCommands
{
    public class ShowIdCommand : BaseCommand
    {
        public override string Name => "my-id";
        public override string StartingMessage => "...";
        public override string Description => "Shows you your Discord ID. (Useful for admins)";

        protected override async Task Invoke(SocketSlashCommand command)
        {
            await command.FollowupAsync("Your ID: " + command.User.Id);
        }
    }
}
