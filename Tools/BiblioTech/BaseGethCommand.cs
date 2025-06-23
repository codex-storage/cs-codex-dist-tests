using BiblioTech.Options;
using CodexContractsPlugin;
using GethPlugin;

namespace BiblioTech
{
    public abstract class BaseGethCommand : BaseCommand
    {
        protected override async Task Invoke(CommandContext context)
        {
            var gethConnector = GetGeth();
            if (gethConnector == null)
            {
                await context.Followup("Blockchain operations are (temporarily) unavailable.");
                return;
            }

            var gethNode = gethConnector.GethNode;
            var contracts = gethConnector.CodexContracts;

            if (!contracts.IsDeployed())
            {
                await context.Followup("I'm sorry, the Codex SmartContracts are not currently deployed.");
                return;
            }

            await Execute(context, gethNode, contracts);
        }

        private GethConnector.GethConnector? GetGeth()
        {
            try
            {
                return GethConnector.GethConnector.Initialize(Program.Log);
            }
            catch (Exception ex)
            {
                Program.Log.Error("Failed to initialize geth connector: " + ex);
                return null;
            }
        }

        protected abstract Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts);
    }
}
