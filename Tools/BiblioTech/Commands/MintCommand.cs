using CodexContractsPlugin;
using Core;
using Discord.WebSocket;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class MintCommand : BaseNetCommand
    {
        private readonly string nl = Environment.NewLine;
        private readonly Ether defaultEthToSend = 10.Eth();
        private readonly TestToken defaultTestTokensToMint = 1024.TestTokens();
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, mint tokens for this user. (Optional, admin-only)",
            isRequired: true);

        public MintCommand(DeploymentsFilesMonitor monitor, CoreInterface ci)
            : base(monitor, ci)
        {
        }

        public override string Name => "mint";
        public override string StartingMessage => "Minting some tokens...";
        public override string Description => "Mint some TestTokens and send some Eth to the user if their balance is low.";
        public override CommandOption[] Options => new[] { optionalUser };

        protected override async Task Execute(SocketSlashCommand command, IGethNode gethNode, ICodexContracts contracts)
        {
            var userId = GetUserId(optionalUser, command);
            var addr = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (addr == null) return;

            var report =
                ProcessEth(gethNode, addr) +
                ProcessTestTokens(gethNode, contracts, addr);

            await command.FollowupAsync(report);
        }

        private string ProcessTestTokens(IGethNode gethNode, ICodexContracts contracts, EthAddress addr)
        {
            var testTokens = contracts.GetTestTokenBalance(gethNode, addr);
            if (testTokens.Amount < 64m)
            {
                contracts.MintTestTokens(gethNode, addr, defaultTestTokensToMint);
                return $"Minted {defaultTestTokensToMint}." + nl;
            }
            return "TestToken balance over threshold." + nl;
        }

        private string ProcessEth(IGethNode gethNode, EthAddress addr)
        {
            var eth = gethNode.GetEthBalance(addr);
            if (eth.Eth < 1.0m)
            {
                gethNode.SendEth(addr, defaultEthToSend);
                return $"Sent {defaultEthToSend}." + nl;
            }

            return "Eth balance over threshold." + nl;
        }
    }
}
