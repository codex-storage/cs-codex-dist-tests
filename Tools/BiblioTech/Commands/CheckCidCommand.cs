using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class CheckCidCommand : BaseCommand
    {
        private readonly StringOption cidOption = new StringOption(
            name: "cid",
            description: "Codex Content-Identifier",
            isRequired: true);
        private readonly CodexCidChecker checker;

        public CheckCidCommand(CodexCidChecker checker)
        {
            this.checker = checker;
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
            await context.Followup(response.Message);
        }
    }
}
