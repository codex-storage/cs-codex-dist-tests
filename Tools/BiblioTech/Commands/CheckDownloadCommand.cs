using BiblioTech.CodexChecking;
using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class CheckDownloadCommand : BaseCommand
    {
        private readonly CodexTwoWayChecker checker;

        private readonly StringOption contentOption = new StringOption(
            name: "content",
            description: "Content of the downloaded file",
            isRequired: false);

        public CheckDownloadCommand(CodexTwoWayChecker checker)
        {
            this.checker = checker;
        }

        public override string Name => "checkdownload";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Checks the download connectivity of your Codex node.";
        public override CommandOption[] Options => [contentOption];

        protected override async Task Invoke(CommandContext context)
        {
            var user = context.Command.User;
            var content = await contentOption.Parse(context);
            try
            {
                var handler = new CheckResponseHandler(context, user);
                if (string.IsNullOrEmpty(content))
                {
                    await checker.StartDownloadCheck(handler, user.Id);
                }
                else
                {
                    await checker.VerifyDownloadCheck(handler, user.Id, content);
                }
            }
            catch (Exception ex)
            {
                await RespondWithError(context, ex);
            }
        }

        private async Task RespondWithError(CommandContext context, Exception ex)
        {
            await Program.AdminChecker.SendInAdminChannel("Exception during CheckDownloadCommand: " + ex);
            await context.Followup("I'm sorry to report something has gone wrong in an unexpected way. Error details are already posted in the admin channel.");
        }
    }
}
