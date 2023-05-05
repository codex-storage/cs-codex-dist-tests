using k8s;

namespace KubernetesWorkflow
{
    public class K8sClient
    {
        private readonly Kubernetes client;
        private static readonly object clientLock = new object();

        public K8sClient(KubernetesClientConfiguration config)
        {
            client = new Kubernetes(config);
        }

        public void Run(Action<Kubernetes> action)
        {
            lock (clientLock)
            {
                action(client);
            }
        }

        public T Run<T>(Func<Kubernetes, T> action)
        {
            lock (clientLock)
            {
                return action(client);
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
