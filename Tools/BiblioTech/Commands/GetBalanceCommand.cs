using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class GetBalanceCommand : BaseGethCommand
    {
        private readonly UserAssociateCommand userAssociateCommand;
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, get balance for another user. (Optional, admin-only)",
            isRequired: false);

        public GetBalanceCommand(UserAssociateCommand userAssociateCommand)
        {
            this.userAssociateCommand = userAssociateCommand;
        }

        public override string Name => "balance";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Shows Eth and TestToken balance of an eth address.";
        public override CommandOption[] Options => new[] { optionalUser };

        protected override async Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts)
        {
            var userId = GetUserFromCommand(optionalUser, context);
            var addr = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (addr == null)
            {
                await context.Followup($"No address has been set for this user. Please use '/{userAssociateCommand.Name}' to set it first.");
                await Program.AdminChecker.SendInAdminChannel($"User {Mention(userId)} used '/{Name}' but address has not been set.");
                return;
            }

            var eth = 0.Eth();
            var testTokens = 0.TstWei();

            await Task.Run(() =>
            {
                eth = gethNode.GetEthBalance(addr);
                testTokens = contracts.GetTestTokenBalance(addr);
            });

            await context.Followup($"{context.Command.User.Username} has {eth} and {testTokens}.");
        }
    }
}
