using k8s;
using Logging;

namespace KubernetesWorkflow
{
    public class CrashWatcher
    {
        private readonly ILog log;
        private readonly KubernetesClientConfiguration config;
        private readonly string k8sNamespace;
        private readonly RunningContainer container;
        private ILogHandler? logHandler;
        private CancellationTokenSource cts;
        private Task? worker;
        private Exception? workerException;

        public CrashWatcher(ILog log, KubernetesClientConfiguration config, string k8sNamespace, RunningContainer container)
        {
            this.log = log;
            this.config = config;
            this.k8sNamespace = k8sNamespace;
            this.container = container;
            cts = new CancellationTokenSource();
        }

        public void Start(ILogHandler logHandler)
        {
            if (worker != null) throw new InvalidOperationException();

            this.logHandler = logHandler;
            cts = new CancellationTokenSource();
            worker = Task.Run(Worker);
        }

        public void Stop()
        {
            if (worker == null) throw new InvalidOperationException();

            cts.Cancel();
            worker.Wait();
            worker = null;

            if (workerException != null) throw new Exception("Exception occurred in CrashWatcher worker thread.", workerException);
        }

        public bool HasContainerCrashed()
        {
            using var client = new Kubernetes(config);
            return HasContainerBeenRestarted(client, container.Pod.PodInfo.Name);
        }

        private void Worker()
        {
            try
            {
                MonitorContainer(cts.Token);
            }
            catch (Exception ex)
            {
                workerException = ex;
            }
        }

        private void MonitorContainer(CancellationToken token)
        {
            using var client = new Kubernetes(config);
            while (!token.IsCancellationRequested)
            {
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                var pod = container.Pod;
                var recipe = container.Recipe;
                var podName = pod.PodInfo.Name;
                if (HasContainerBeenRestarted(client, podName))
                {
                    DownloadCrashedContainerLogs(client, podName, recipe);
                    return;
                }
            }
        }

        private bool HasContainerBeenRestarted(Kubernetes client, string podName)
        {
            var podInfo = client.ReadNamespacedPod(podName, k8sNamespace);
            return podInfo.Status.ContainerStatuses.Any(c => c.RestartCount > 0);
        }

        private void DownloadCrashedContainerLogs(Kubernetes client, string podName, ContainerRecipe recipe)
        {
            log.Log("Pod crash detected for " + container.Name);
            using var stream = client.ReadNamespacedPodLog(podName, k8sNamespace, recipe.Name, previous: true);
            logHandler!.Log(stream);
        }
    }
}
