using BiblioTech.Options;
using CodexContractsPlugin;
using CodexPlugin;
using Core;
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
                return;
            }

            var eth = gethNode.GetEthBalance(addr);
            var testTokens = contracts.GetTestTokenBalance(addr);

            await context.Followup($"{context.Command.User.Username} has {eth} and {testTokens}.");
        }
    }
}
