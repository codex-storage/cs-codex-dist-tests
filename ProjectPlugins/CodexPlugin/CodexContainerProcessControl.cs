using CodexClient;
using Core;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;

namespace CodexPlugin
{
    public class CodexContainerProcessControl : IProcessControl
    {
        private readonly IPluginTools tools;
        private readonly RunningPod pod;
        private readonly Action onStop;
        private readonly ContainerCrashWatcher crashWatcher;

        public CodexContainerProcessControl(IPluginTools tools, RunningPod pod, Action onStop)
        {
            this.tools = tools;
            this.pod = pod;
            this.onStop = onStop;

            crashWatcher = tools.CreateWorkflow().CreateCrashWatcher(pod.Containers.Single());
            crashWatcher.Start();
        }

        public void Stop(bool waitTillStopped)
        {
            Log($"Stopping node...");
            crashWatcher.Stop();
            var workflow = tools.CreateWorkflow();
            workflow.Stop(pod, waitTillStopped);
            onStop();
            Log("Stopped.");
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            var workflow = tools.CreateWorkflow();
            return workflow.DownloadContainerLog(pod.Containers.Single());
        }

        public void DeleteDataDirFolder()
        {
            var container = pod.Containers.Single();

            try
            {
                var dataDirVar = container.Recipe.EnvVars.Single(e => e.Name == "CODEX_DATA_DIR");
                var dataDir = dataDirVar.Value;
                var workflow = tools.CreateWorkflow();
                workflow.ExecuteCommand(container, "rm", "-Rfv", $"/codex/{dataDir}/repo");
                Log("Deleted repo folder.");
            }
            catch (Exception e)
            {
                Log("Unable to delete repo folder: " + e);
            }
        }

        public bool HasCrashed()
        {
            return crashWatcher.HasCrashed();
        }

        private void Log(string message)
        {
            tools.GetLog().Log(message);
        }
    }
}
