﻿using k8s;
using Logging;

namespace KubernetesWorkflow
{
    public class ContainerCrashWatcher
    {
        private readonly ILog log;
        private readonly KubernetesClientConfiguration config;
        private readonly string containerName;
        private readonly string podName;
        private readonly string recipeName;
        private readonly string k8sNamespace;
        private CancellationTokenSource cts;
        private Task? worker;
        private Exception? workerException;

        public ContainerCrashWatcher(ILog log, KubernetesClientConfiguration config, string containerName, string podName, string recipeName, string k8sNamespace)
        {
            this.log = log;
            this.config = config;
            this.containerName = containerName;
            this.podName = podName;
            this.recipeName = recipeName;
            this.k8sNamespace = k8sNamespace;
            cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (worker != null) throw new InvalidOperationException();

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

        public bool HasCrashed()
        {
            using var client = new Kubernetes(config);
            var result = HasContainerBeenRestarted(client);
            if (result) DownloadCrashedContainerLogs(client);
            return result;
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
                token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                if (HasContainerBeenRestarted(client))
                {
                    DownloadCrashedContainerLogs(client);
                    return;
                }
            }
        }

        private bool HasContainerBeenRestarted(Kubernetes client)
        {
            var podInfo = client.ReadNamespacedPod(podName, k8sNamespace);
            var result = podInfo.Status.ContainerStatuses.Any(c => c.RestartCount > 0);
            if (result) log.Log("Pod crash detected for " + containerName);
            return result;
        }

        private void DownloadCrashedContainerLogs(Kubernetes client)
        {
            using var stream = client.ReadNamespacedPodLog(podName, k8sNamespace, recipeName, previous: true);
            var handler = new WriteToFileLogHandler(log, "Crash detected for " + containerName, containerName);
            handler.Log(stream);
        }
    }
}
