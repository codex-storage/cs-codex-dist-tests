using BiblioTech.Options;
using CodexContractsPlugin;
using Core;
using GethPlugin;

namespace BiblioTech
{
    public abstract class BaseNetCommand : BaseCommand
    {
        private readonly DeploymentsFilesMonitor monitor;
        private readonly CoreInterface ci;

        public BaseNetCommand(DeploymentsFilesMonitor monitor, CoreInterface ci)
        {
            this.monitor = monitor;
            this.ci = ci;
        }

        protected override async Task Invoke(CommandContext context)
        {
            var deployments = monitor.GetDeployments();
            if (deployments.Length == 0)
            {
                await context.Followup("No deployments are currently available.");
                return;
            }
            if (deployments.Length > 1) 
            {
                await context.Followup("Multiple deployments are online. I don't know which one to pick!");
                return;
            }

            var codexDeployment = deployments.Single();
            var gethDeployment = codexDeployment.GethDeployment;
            var contractsDeployment = codexDeployment.CodexContractsDeployment;

            var gethNode = ci.WrapGethDeployment(gethDeployment);
            var contracts = ci.WrapCodexContractsDeployment(contractsDeployment);

            await Execute(context, gethNode, contracts);
        }

        protected abstract Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts);
    }
}
