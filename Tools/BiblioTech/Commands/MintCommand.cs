using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech.Commands
{
    public class MintCommand : BaseGethCommand
    {
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

            Transaction<Ether>? sentEth = null;
            Transaction<TestToken>? mintedTokens = null;

            await Task.Run(() =>
            {
                sentEth = ProcessEth(gethNode, addr, report);
                mintedTokens = ProcessTokens(contracts, addr, report);
            });

            Program.UserRepo.AddMintEventForUser(userId, addr, sentEth, mintedTokens);

            await context.Followup(string.Join(Environment.NewLine, report));
        }

        private Transaction<TestToken>? ProcessTokens(ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            if (ShouldMintTestTokens(contracts, addr))
            {
                var tokens = Program.Config.MintTT.TestTokens();
                var transaction = contracts.MintTestTokens(addr, tokens);
                report.Add($"Minted {tokens} {FormatTransactionLink(transaction)}");
                return new Transaction<TestToken>(tokens, transaction);
            }
            
            report.Add("TestToken balance over threshold. (No TestTokens minted.)");
            return null;
        }

        private Transaction<Ether>? ProcessEth(IGethNode gethNode, EthAddress addr, List<string> report)
        {
            if (ShouldSendEth(gethNode, addr))
            {
                var eth = Program.Config.SendEth.Eth();
                var transaction = gethNode.SendEth(addr, eth);
                report.Add($"Sent {eth} {FormatTransactionLink(transaction)}");
                return new Transaction<Ether>(eth, transaction);
            }
            report.Add("Eth balance is over threshold. (No Eth sent.)");
            return null;
        }

        private bool ShouldMintTestTokens(ICodexContracts contracts, EthAddress addr)
        {
            var testTokens = contracts.GetTestTokenBalance(addr);
            return testTokens.Amount < Program.Config.MintTT.TestTokens().Amount;
        }

        private bool ShouldSendEth(IGethNode gethNode, EthAddress addr)
        {
            var eth = gethNode.GetEthBalance(addr);
            return eth.Eth < Program.Config.SendEth;
        }

        private string FormatTransactionLink(string transaction)
        {
            var url = $"https://explorer.testnet.codex.storage/tx/{transaction}";
            return $"- [View on block explorer](<{url}>){Environment.NewLine}Transaction ID - `{transaction}`";
        }
    }
}
