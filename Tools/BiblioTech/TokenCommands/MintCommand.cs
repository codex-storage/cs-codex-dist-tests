using CodexContractsPlugin;
using Core;
using Discord.WebSocket;
using GethPlugin;

namespace BiblioTech.TokenCommands
{
    public class MintCommand : BaseNetCommand
    {
        private readonly string nl = Environment.NewLine;
        private readonly Ether defaultEthToSend = 10.Eth();
        private readonly TestToken defaultTestTokensToMint = 1024.TestTokens();
        private readonly EthAddressOption ethOption = new EthAddressOption();

        public MintCommand(DeploymentsFilesMonitor monitor, CoreInterface ci)
            : base(monitor, ci)
        {
        }

        public override string Name => "mint";
        public override string Description => "Mint some TestTokens and send some Eth to the address if its balance is low.";
        public override CommandOption[] Options => new[] { ethOption };

        protected override async Task Execute(SocketSlashCommand command, IGethNode gethNode, ICodexContracts contracts)
        {
            var addr = await ethOption.Parse(command);
            if (addr == null) return;

            var report = 
                ProcessEth(gethNode, addr) +
                ProcessTestTokens(gethNode, contracts, addr);

            await command.RespondAsync(report);
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
