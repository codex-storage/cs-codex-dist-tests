using BiblioTech.Options;
using CodexContractsPlugin;
using CodexPlugin;
using Core;
using GethPlugin;

namespace BiblioTech
{
    public abstract class BaseGethCommand : BaseDeploymentCommand
    {
        private readonly CoreInterface ci;

        public BaseGethCommand(CoreInterface ci)
        {
            this.ci = ci;
        }

        protected override async Task ExecuteDeploymentCommand(CommandContext context, CodexDeployment codexDeployment)
        {
            var gethDeployment = codexDeployment.GethDeployment;
            var contractsDeployment = codexDeployment.CodexContractsDeployment;

            var gethNode = ci.WrapGethDeployment(gethDeployment);
            var contracts = ci.WrapCodexContractsDeployment(gethNode, contractsDeployment);

            await Execute(context, gethNode, contracts);
        }

        protected abstract Task Execute(CommandContext context, IGethNode gethNode, ICodexContracts contracts);
    }
}
