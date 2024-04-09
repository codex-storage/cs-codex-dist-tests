using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech
{
    public abstract class BaseGethCommand : BaseCommand
    {
        protected override async Task Invoke(CommandContext context)
        {
            var gethConnector = GethConnector.GethConnector.Initialize(Program.Log);

            if (gethConnector == null) return;
            var gethNode = gethConnector.GethNode;
            var contracts = gethConnector.CodexContracts;

            if (!contracts.IsDeployed())
            {
                await context.Followup("I'm sorry, the Codex SmartContracts are not currently deployed.");
                return;
            }

            await Execute(context, gethNode, contracts);
        }

        protected abstract Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts);
    }
}
