using CodexContractsPlugin;
using Core;
using Discord.WebSocket;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class GetBalanceCommand : BaseNetCommand
    {
        private readonly UserAssociateCommand userAssociateCommand;
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, get balance for another user. (Optional, admin-only)",
            isRequired: false);

        public GetBalanceCommand(DeploymentsFilesMonitor monitor, CoreInterface ci, UserAssociateCommand userAssociateCommand)
            : base(monitor, ci)
        {
            this.userAssociateCommand = userAssociateCommand;
        }

        public override string Name => "balance";
        public override string StartingMessage => "Fetching balance...";
        public override string Description => "Shows Eth and TestToken balance of an eth address.";
        public override CommandOption[] Options => new[] { optionalUser };

        protected override async Task Execute(SocketSlashCommand command, IGethNode gethNode, ICodexContracts contracts)
        {
            var userId = GetUserId(optionalUser, command);
            var addr = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (addr == null)
            {
                await command.FollowupAsync($"No address has been set for this user. Please use '/{userAssociateCommand.Name}' to set it first.");
                return;
            }

            var eth = gethNode.GetEthBalance(addr);
            var testTokens = contracts.GetTestTokenBalance(gethNode, addr);

            await command.FollowupAsync($"{command.User.Username} has {eth} and {testTokens}.");
        }
    }
}
