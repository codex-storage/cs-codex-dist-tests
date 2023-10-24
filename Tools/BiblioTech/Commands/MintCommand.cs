using BiblioTech.Options;
using CodexContractsPlugin;
using Core;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class MintCommand : BaseNetCommand
    {
        private readonly Ether defaultEthToSend = 10.Eth();
        private readonly TestToken defaultTestTokensToMint = 1024.TestTokens();
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, mint tokens for this user. (Optional, admin-only)",
            isRequired: false);
        private readonly UserAssociateCommand userAssociateCommand;

        public MintCommand(DeploymentsFilesMonitor monitor, CoreInterface ci, UserAssociateCommand userAssociateCommand)
            : base(monitor, ci)
        {
            this.userAssociateCommand = userAssociateCommand;
        }

        public override string Name => "mint";
        public override string StartingMessage => "Minting some tokens...";
        public override string Description => "Mint some TestTokens and send some Eth to the user if their balance is low.";
        public override CommandOption[] Options => new[] { optionalUser };

        protected override async Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts)
        {
            var userId = GetUserId(optionalUser, context);
            var addr = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (addr == null)
            {
                await context.Command.FollowupAsync($"No address has been set for this user. Please use '/{userAssociateCommand.Name}' to set it first.");
                return;
            }

            var report = new List<string>();

            var sentEth = ProcessEth(gethNode, addr, report);
            var mintedTokens = ProcessTokens(gethNode, contracts, addr, report);

            Program.UserRepo.AddMintEventForUser(userId, addr, sentEth, mintedTokens);

            await context.Command.FollowupAsync(string.Join(Environment.NewLine, report));
        }

        private TestToken ProcessTokens(IGethNode gethNode, ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            if (ShouldMintTestTokens(gethNode, contracts, addr))
            {
                contracts.MintTestTokens(gethNode, addr, defaultTestTokensToMint);
                report.Add($"Minted {defaultTestTokensToMint}.");
                return defaultTestTokensToMint;
            }
            
            report.Add("TestToken balance over threshold.");
            return 0.TestTokens();
        }

        private Ether ProcessEth(IGethNode gethNode, EthAddress addr, List<string> report)
        {
            if (ShouldSendEth(gethNode, addr))
            {
                gethNode.SendEth(addr, defaultEthToSend);
                report.Add($"Sent {defaultEthToSend}.");
                return defaultEthToSend;
            }
            report.Add("Eth balance is over threshold.");
            return 0.Eth();
        }

        private bool ShouldMintTestTokens(IGethNode gethNode, ICodexContracts contracts, EthAddress addr)
        {
            var testTokens = contracts.GetTestTokenBalance(gethNode, addr);
            return testTokens.Amount < 64m;
        }

        private bool ShouldSendEth(IGethNode gethNode, EthAddress addr)
        {
            var eth = gethNode.GetEthBalance(addr);
            return eth.Eth < 1.0m;
        }
    }
}
