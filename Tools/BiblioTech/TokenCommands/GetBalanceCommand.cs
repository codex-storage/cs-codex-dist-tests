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

        public GetBalanceCommand(DeploymentsFilesMonitor monitor, CoreInterface ci)
            : base(monitor, ci)
        {
        }

        public override string Name => "balance";
        public override string StartingMessage => "Fetching balance...";
        public override string Description => "Shows Eth and TestToken balance of an eth address.";
        public override CommandOption[] Options => new[] { ethOption };

        protected override async Task Execute(SocketSlashCommand command, IGethNode gethNode, ICodexContracts contracts)
        {
            var addr = await ethOption.Parse(command);
            if (addr == null) return;

            var eth = gethNode.GetEthBalance(addr);
            var testTokens = contracts.GetTestTokenBalance(gethNode, addr);

            await command.RespondAsync($"Address '{addr.Address}' has {eth} and {testTokens}.");
        }
    }
}
