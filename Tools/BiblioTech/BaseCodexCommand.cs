using BiblioTech.Options;
using CodexPlugin;
using Core;

namespace BiblioTech
{
    public abstract class BaseCodexCommand : BaseDeploymentCommand
    {
        private readonly CoreInterface ci;

        public BaseCodexCommand(CoreInterface ci)
        {
            this.ci = ci;
        }

        protected override async Task ExecuteDeploymentCommand(CommandContext context, CodexDeployment codexDeployment)
        {
            var codexContainers = codexDeployment.CodexInstances.Select(c => c.Containers).ToArray();

            var group = ci.WrapCodexContainers(codexContainers);

            await Execute(context, group);
        }

        protected abstract Task Execute(CommandContext context, ICodexNodeGroup codexGroup);
    }
}
