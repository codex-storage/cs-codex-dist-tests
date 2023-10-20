using CodexPlugin;
using Discord.WebSocket;

namespace BiblioTech
{
    public abstract class BaseNetCommand : BaseCommand
    {
        private readonly DeploymentsFilesMonitor monitor;

        public BaseNetCommand(DeploymentsFilesMonitor monitor)
        {
            this.monitor = monitor;
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

            await Execute(command, deployments.Single());
        }

        protected abstract Task Execute(SocketSlashCommand command, CodexDeployment codexDeployment);
    }
}
