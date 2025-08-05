using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;
using Logging;
using Utils;

namespace BiblioTech.Commands
{
    public class MintCommand : BaseGethCommand
    {
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, mint tokens for this user. (Optional, admin-only)",
            isRequired: false);
        private readonly UserAssociateCommand userAssociateCommand;
        private readonly ILog log;

        public MintCommand(UserAssociateCommand userAssociateCommand)
        {
            this.userAssociateCommand = userAssociateCommand;
            log = Program.Log;
        }

        public override string Name => "mint";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Transfer and/or mint some TestTokens and Eth to the user if their balance is low.";
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

            log.Debug($"Running mint command for {userId} with address {addr}...");
            var report = new List<string>();

            Transaction<Ether>? sentEth = null;
            Transaction<TestToken>? mintedTokens = null;

            await Task.Run(async () =>
            {
                sentEth = ProcessEth(gethNode, addr, report);
                mintedTokens = await ProcessTestTokens(contracts, addr, report);
            });

            var reportLine = string.Join(Environment.NewLine, report);
            Program.UserRepo.AddMintEventForUser(userId, addr, sentEth, mintedTokens);
            await Program.AdminChecker.SendInAdminChannel($"User {Mention(userId)} used '/{Name}' successfully. ({reportLine})");

            await context.Followup(reportLine);
        }

        private async Task<Transaction<TestToken>?> ProcessTestTokens(ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            if (IsTestTokenBalanceOverLimit(contracts, addr))
            {
                log.Debug("TestToken balance is over threshold.");
                report.Add("TestToken balance over threshold. (No TestTokens sent or minted.)");
                return null;
            }

            var sent = await TransferTestTokens(contracts, addr, report);
            var minted = await MintTestTokens(contracts, addr, report);

            return new Transaction<TestToken>(sent.Item1, minted.Item1, $"{sent.Item2},{minted.Item2}");
        }

        private Transaction<Ether>? ProcessEth(IGethNode gethNode, EthAddress addr, List<string> report)
        {
            if (IsEthBalanceOverLimit(gethNode, addr))
            {
                log.Debug("Eth balance is over threshold.");
                report.Add("Eth balance is over threshold. (No Eth sent.)");
                return null;
            }
            var eth = Program.Config.SendEth.Eth();
            log.Debug($"Sending {eth}...");
            var transaction = gethNode.SendEth(addr, eth);
            report.Add($"Sent {eth} {FormatTransactionLink(transaction)}");
            return new Transaction<Ether>(eth, 0.Eth(), transaction);
        }

        private async Task<(TestToken, string)> MintTestTokens(ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            var nothing = (0.TstWei(), string.Empty);
            if (Program.Config.MintTT < 1)
            {
                log.Debug("Skip minting TST: configured amount is less than 1.");
                return nothing;
            }

            try
            {
                var tokens = Program.Config.MintTT.TstWei();
                log.Debug($"Minting {tokens}...");
                var transaction = contracts.MintTestTokens(addr, tokens);
                report.Add($"Minted {tokens} {FormatTransactionLink(transaction)}");
                return (tokens, transaction);
            }
            catch (Exception ex)
            {
                report.Add("Minter: I'm sorry! Something went unexpectedly wrong. (Admins have been notified)");
                await Program.AdminChecker.SendInAdminChannel($"{nameof(MintCommand)} {nameof(MintTestTokens)} failed with: {ex}");
            }
            return nothing;
        }

        private async Task<(TestToken, string)> TransferTestTokens(ICodexContracts contracts, EthAddress addr, List<string> report)
        {
            var nothing = (0.TstWei(), string.Empty);
            if (Program.Config.SendTT < 1)
            {
                log.Debug("Skip transferring TST: configured amount is less than 1.");
                return nothing;
            }
            if (Program.GethLink == null)
            {
                log.Debug("Skip transferring TST: GethLink not available.");
                report.Add("Transaction operations are currently not available.");
                return nothing;
            }

            try
            {
                var current = contracts.GetTestTokenBalance(Program.GethLink.Node.CurrentAddress);
                var amount = Program.Config.SendTT.TstWei();
                if (current.TstWei <= amount.TstWei)
                {
                    log.Debug($"Unable to transfer TST: Bot has: {current} - Transfer amount: {amount}");
                    report.Add("Unable to send TestTokens: Bot doesn't have enough! (Admins have been notified)");
                    await Program.AdminChecker.SendInAdminChannel($"{nameof(MintCommand)} failed: Bot has insufficient tokens.");
                    return nothing;
                }

                log.Debug($"Sending {amount}...");
                var transaction = contracts.TransferTestTokens(addr, amount);
                report.Add($"Transferred {amount} {FormatTransactionLink(transaction)}");
                return (amount, transaction);
            }
            catch (Exception ex)
            {
                report.Add("Transfer: I'm sorry! Something went unexpectedly wrong. (Admins have been notified)");
                await Program.AdminChecker.SendInAdminChannel($"{nameof(MintCommand)} {nameof(TransferTestTokens)} failed with: {ex}");
            }
            return nothing;
        }

        private bool IsEthBalanceOverLimit(IGethNode gethNode, EthAddress addr)
        {
            var eth = gethNode.GetEthBalance(addr);
            return ((decimal)eth.Eth) > Program.Config.SendEth;
        }

        private bool IsTestTokenBalanceOverLimit(ICodexContracts contracts, EthAddress addr)
        {
            var testTokens = contracts.GetTestTokenBalance(addr);

            if (Program.Config.MintTT > 0 && testTokens > Program.Config.MintTT.TstWei()) return true;
            if (Program.Config.SendTT > 0 && testTokens > Program.Config.SendTT.TstWei()) return true;

            return false;
        }

        private string FormatTransactionLink(string transaction)
        {
            var url = Program.Config.TransactionLinkFormat.Replace("<ID>", transaction);
            return $"- [View on block explorer](<{url}>){Environment.NewLine}Transaction ID - `{transaction}`";
        }
    }
}
