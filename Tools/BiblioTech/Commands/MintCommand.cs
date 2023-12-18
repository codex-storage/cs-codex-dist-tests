using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class MintCommand : BaseGethCommand
    {
        private readonly Ether defaultEthToSend = 10.Eth();
        private readonly TestToken defaultTestTokensToMint = 1024.TestTokens();
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, mint tokens for this user. (Optional, admin-only)",
            isRequired: false);
        private readonly UserAssociateCommand userAssociateCommand;

        public MintCommand(UserAssociateCommand userAssociateCommand)
        {
            this.userAssociateCommand = userAssociateCommand;
        }

        public override string Name => "mint";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Mint some TestTokens and send some Eth to the user if their balance is low.";
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

            var report = new List<string>();

            var sentEth = ProcessEth(gethNode, addr, report);
            var mintedTokens = ProcessTokens(contracts, addr, report);

            Program.UserRepo.AddMintEventForUser(userId, addr, sentEth, mintedTokens);

            await context.Followup(string.Join(Environment.NewLine, report));
        }

        private Transaction<TestToken>? ProcessTokens(ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            if (ShouldMintTestTokens(contracts, addr))
            {
                var transaction = contracts.MintTestTokens(addr, defaultTestTokensToMint);
                report.Add($"Minted {defaultTestTokensToMint} {FormatTransactionLink(transaction)}");
                return new Transaction<TestToken>(defaultTestTokensToMint, transaction);
            }
            
            report.Add("TestToken balance over threshold. (No TestTokens minted.)");
            return null;
        }

        private Transaction<Ether>? ProcessEth(IGethNode gethNode, EthAddress addr, List<string> report)
        {
            if (ShouldSendEth(gethNode, addr))
            {
                var transaction = gethNode.SendEth(addr, defaultEthToSend);
                report.Add($"Sent {defaultEthToSend} {FormatTransactionLink(transaction)}");
                return new Transaction<Ether>(defaultEthToSend, transaction);
            }
            report.Add("Eth balance is over threshold. (No Eth sent.)");
            return null;
        }

        private bool ShouldMintTestTokens(ICodexContracts contracts, EthAddress addr)
        {
            var testTokens = contracts.GetTestTokenBalance(addr);
            return testTokens.Amount < 64m;
        }

        private bool ShouldSendEth(IGethNode gethNode, EthAddress addr)
        {
            var eth = gethNode.GetEthBalance(addr);
            return eth.Eth < 1.0m;
        }

        private string FormatTransactionLink(string transaction)
        {
            var url = $"https://explorer.testnet.codex.storage/tx/{transaction}";
            return $"- [View on block explorer]({url}){Environment.NewLine}Transaction ID - `{transaction}`";
        }
    }
}
