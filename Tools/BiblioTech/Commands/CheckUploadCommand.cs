using BiblioTech.CodexChecking;
using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class CheckUploadCommand : BaseCommand
    {
        private readonly CodexTwoWayChecker checker;

        private readonly StringOption cidOption = new StringOption(
            name: "cid",
            description: "Codex Content-Identifier",
            isRequired: false);

        public CheckUploadCommand(CodexTwoWayChecker checker)
        {
            this.checker = checker;
        }

        public override string Name => "checkupload";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Checks the upload connectivity of your Codex node.";
        public override CommandOption[] Options => [cidOption];

        protected override async Task Invoke(CommandContext context)
        {
            var user = context.Command.User;
            var cid = await cidOption.Parse(context);
            try
            {
                var handler = new CheckResponseHandler(context, user);
                if (string.IsNullOrEmpty(cid))
                {
                    await checker.StartUploadCheck(handler, user.Id);
                }
                else
                {
                    await checker.VerifyUploadCheck(handler, user.Id, cid);
                }
            }
            catch (Exception ex)
            {
                await RespondWithError(context, ex);
            }
        }

        private async Task RespondWithError(CommandContext context, Exception ex)
        {
            await Program.AdminChecker.SendInAdminChannel("Exception during CheckUploadCommand: " + ex);
            await context.Followup("I'm sorry to report something has gone wrong in an unexpected way. Error details are already posted in the admin channel.");
        }
    }
}
