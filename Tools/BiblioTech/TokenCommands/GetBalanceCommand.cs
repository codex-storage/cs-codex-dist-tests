using CodexContractsPlugin;
using CodexPlugin;
using Core;
using Discord.WebSocket;
using GethPlugin;

namespace BiblioTech.TokenCommands
{
    public class GetBalanceCommand : BaseNetCommand
    {
        private readonly EthAddressOption ethOption = new EthAddressOption();
        private readonly CoreInterface ci;

        public GetBalanceCommand(DeploymentsFilesMonitor monitor, CoreInterface ci)
            : base(monitor)
        {
            this.ci = ci;
        }

        public override string Name => "balance";
        public override string Description => "Shows Eth and TestToken balance of an eth address.";
        public override CommandOption[] Options => new[] { ethOption };

        protected override async Task Execute(SocketSlashCommand command, CodexDeployment codexDeployment)
        {
            var addr = await ethOption.Parse(command);
            if (addr == null) return;

            var gethDeployment = codexDeployment.GethDeployment;
            var contractsDeployment = codexDeployment.CodexContractsDeployment;

            var gethNode = ci.WrapGethDeployment(gethDeployment);
            var contracts = ci.WrapCodexContractsDeployment(contractsDeployment);

            var eth = gethNode.GetEthBalance(addr);
            var testTokens = contracts.GetTestTokenBalance(gethNode, addr);

            await command.RespondAsync($"Address '{addr.Address}' has {eth} and {testTokens}.");
        }
    }
}
