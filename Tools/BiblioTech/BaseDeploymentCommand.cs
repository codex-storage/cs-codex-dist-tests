using BiblioTech.Options;
using CodexPlugin;

namespace BiblioTech
{
    public abstract class BaseDeploymentCommand : BaseCommand
    {
        protected override async Task Invoke(CommandContext context)
        {
            var proceed = await OnInvoke(context);
            if (!proceed) return;

            var deployments = Program.DeploymentFilesMonitor.GetDeployments();
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
            await ExecuteDeploymentCommand(context, codexDeployment);
        }

        protected abstract Task ExecuteDeploymentCommand(CommandContext context, CodexDeployment codexDeployment);

        protected virtual Task<bool> OnInvoke(CommandContext context)
        {
            return Task.FromResult(true);
        }
    }
}
