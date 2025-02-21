using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;

namespace Core
{
    public sealed class CoreInterface
    {
        private readonly EntryPoint entryPoint;

        internal CoreInterface(EntryPoint entryPoint)
        {
            this.entryPoint = entryPoint;
        }

        public T GetPlugin<T>() where T : IProjectPlugin
        {
            return entryPoint.GetPlugin<T>();
        }

        public IKnownLocations GetKnownLocations()
        {
            return entryPoint.Tools.CreateWorkflow().GetAvailableLocations();
        }

        public IDownloadedLog DownloadLog(IHasContainer containerSource, int? tailLines = null)
        {
            return DownloadLog(containerSource.Container, tailLines);
        }

        public IDownloadedLog DownloadLog(RunningContainer container, int? tailLines = null)
        {
            var workflow = entryPoint.Tools.CreateWorkflow();
            return workflow.DownloadContainerLog(container, tailLines);
        }

        public string ExecuteContainerCommand(IHasContainer containerSource, string command, params string[] args)
        {
            return ExecuteContainerCommand(containerSource.Container, command, args);
        }

        public string ExecuteContainerCommand(RunningContainer container, string command, params string[] args)
        {
            var workflow = entryPoint.Tools.CreateWorkflow();
            return workflow.ExecuteCommand(container, command, args);
        }
    }

    public interface IHasContainer
    {
        RunningContainer Container { get; }
    }
}
