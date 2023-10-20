using CodexContractsPlugin;
using Core;
using Discord.WebSocket;
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

        protected override async Task Invoke(SocketSlashCommand command)
        {
            var deployments = monitor.GetDeployments();
            if (deployments.Length == 0)
            {
                await command.RespondAsync("No deployments are currently available.");
                return;
            }
            if (deployments.Length > 1) 
            {
                await command.RespondAsync("Multiple deployments are online. I don't know which one to pick!");
                return;
            }

            var codexDeployment = deployments.Single();
            var gethDeployment = codexDeployment.GethDeployment;
            var contractsDeployment = codexDeployment.CodexContractsDeployment;

            var gethNode = ci.WrapGethDeployment(gethDeployment);
            var contracts = ci.WrapCodexContractsDeployment(contractsDeployment);

            await Execute(command, gethNode, contracts);
        }

        protected abstract Task Execute(SocketSlashCommand command, IGethNode gethNode, ICodexContracts contracts);
    }
}
