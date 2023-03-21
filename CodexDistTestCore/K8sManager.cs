using k8s;
using k8s.Models;
using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IK8sManager
    {
        IOnlineCodexNodes BringOnline(OfflineCodexNodes node);
        IOfflineCodexNodes BringOffline(IOnlineCodexNodes node);
    }

    public class K8sManager : IK8sManager
    {
        public const string K8sNamespace = "codex-test-namespace";
        private readonly CodexDockerImage dockerImage = new CodexDockerImage();
        private readonly NumberSource activeDeploymentOrderNumberSource = new NumberSource(0);
        private readonly List<OnlineCodexNodes> activeCodexNodes = new List<OnlineCodexNodes>();
        private readonly List<string> knownActivePodNames = new List<string>();
        private readonly IFileManager fileManager;

        public K8sManager(IFileManager fileManager)
        {
            this.fileManager = fileManager;
        }

        public IOnlineCodexNodes BringOnline(OfflineCodexNodes offline)
        {
            var client = CreateClient();
            EnsureTestNamespace(client);

            var containers = CreateContainers(offline.NumberOfNodes);
            var online = containers.Select(c => new OnlineCodexNode(fileManager, c)).ToArray();
            var result = new OnlineCodexNodes(activeDeploymentOrderNumberSource.GetNextNumber(), offline, this, online);
            activeCodexNodes.Add(result);

            CreateDeployment(client, result, offline);
            CreateService(result, client);

            WaitUntilOnline(result, client);
            TestLog.Log($"{offline.NumberOfNodes} Codex nodes online.");

            return result;
        }

        private CodexNodeContainer[] CreateContainers(int number)
        {
            var factory = new CodexNodeContainerFactory();
            var containers = new List<CodexNodeContainer>();
            for (var i = 0; i < number; i++) containers.Add(factory.CreateNext());
            return containers.ToArray();
        }

        public IOfflineCodexNodes BringOffline(IOnlineCodexNodes node)
        {
            var client = CreateClient();

            var activeNode = GetAndRemoveActiveNodeFor(node);

            var deploymentName = activeNode.Deployment.Name();
            BringOffline(activeNode, client);
            WaitUntilOffline(deploymentName, client);
            TestLog.Log($"{activeNode.Describe()} offline.");

            return activeNode.Origin;
        }

        public void DeleteAllResources()
        {
            var client = CreateClient();

            DeleteNamespace(client);

            WaitUntilZeroPods(client);
            WaitUntilNamespaceDeleted(client);
        }

        public void FetchAllPodsLogs(Action<int, string, Stream> onLog)
        {
            var client = CreateClient();
            foreach (var node in activeCodexNodes)
            {
                var nodeDescription = node.Describe();
                foreach (var podName in node.ActivePodNames)
                {
                    var stream = client.ReadNamespacedPodLog(podName, K8sNamespace);
                    onLog(node.OrderNumber, $"{nodeDescription}:{podName}", stream);
                }
            }
        }

        private void BringOffline(OnlineCodexNodes online, Kubernetes client)
        {
            DeleteDeployment(client, online);
            DeleteService(client, online);
        }

        #region Waiting

        private void WaitUntilOnline(OnlineCodexNodes online, Kubernetes client)
        {
            WaitUntil(() =>
            {
                online.Deployment = client.ReadNamespacedDeployment(online.Deployment.Name(), K8sNamespace);
                return online.Deployment?.Status.AvailableReplicas != null && online.Deployment.Status.AvailableReplicas > 0;
            });

            AssignActivePodNames(online, client);
        }

        private void AssignActivePodNames(OnlineCodexNodes online, Kubernetes client)
        {
            var pods = client.ListNamespacedPod(K8sNamespace);
            var podNames = pods.Items.Select(p => p.Name());
            foreach (var podName in podNames)
            {
                if (!knownActivePodNames.Contains(podName))
                {
                    knownActivePodNames.Add(podName);
                    online.ActivePodNames.Add(podName);
                }
            }
        }

        private void WaitUntilOffline(string deploymentName, Kubernetes client)
        {
            WaitUntil(() =>
            {
                var deployment = client.ReadNamespacedDeployment(deploymentName, K8sNamespace);
                return deployment == null || deployment.Status.AvailableReplicas == 0;
            });
        }

        private void WaitUntilZeroPods(Kubernetes client)
        {
            WaitUntil(() => !client.ListNamespacedPod(K8sNamespace).Items.Any());
        }

        private void WaitUntilNamespaceDeleted(Kubernetes client)
        {
            WaitUntil(() => !IsTestNamespaceOnline(client));
        }

        private void WaitUntil(Func<bool> predicate)
        {
            var start = DateTime.UtcNow;
            var state = predicate();
            while (!state)
            {
                if (DateTime.UtcNow - start > Timing.K8sOperationTimeout())
                {
                    Assert.Fail("K8s operation timed out.");
                    throw new TimeoutException();
                }

                Timing.WaitForK8sServiceDelay();
                state = predicate();
            }
        }

        #endregion

        #region Service management

        private void CreateService(OnlineCodexNodes online, Kubernetes client)
        {
            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = online.GetServiceMetadata(),
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = online.GetSelector(),
                    Ports = CreateServicePorts(online)
                }
            };

            online.Service = client.CreateNamespacedService(serviceSpec, K8sNamespace);
        }

        private List<V1ServicePort> CreateServicePorts(OnlineCodexNodes online)
        {
            var result = new List<V1ServicePort>();
            var containers = online.GetContainers();
            foreach (var container in containers)
            {
                result.Add(new V1ServicePort
                {
                    Protocol = "TCP",
                    Port = 8080,
                    TargetPort = container.ContainerPortName,
                    NodePort = container.ServicePort
                });
            }
            return result;
        }

        private void DeleteService(Kubernetes client, OnlineCodexNodes online)
        {
            if (online.Service == null) return;
            client.DeleteNamespacedService(online.Service.Name(), K8sNamespace);
            online.Service = null;
        }

        #endregion

        #region Deployment management

        private void CreateDeployment(Kubernetes client, OnlineCodexNodes online, OfflineCodexNodes offline)
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = online.GetDeploymentMetadata(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = online.GetSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = online.GetSelector()
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = CreateDeploymentContainers(online, offline)
                        }
                    }
                }
            };

            online.Deployment = client.CreateNamespacedDeployment(deploymentSpec, K8sNamespace);
        }

        private List<V1Container> CreateDeploymentContainers(OnlineCodexNodes online, OfflineCodexNodes offline)
        {
            var result = new List<V1Container>();
            var containers = online.GetContainers();
            foreach (var container in containers)
            {
                result.Add(new V1Container
                {
                    Name = container.Name,
                    Image = dockerImage.GetImageTag(),
                    Ports = new List<V1ContainerPort>
                    {
                        new V1ContainerPort
                        {
                            ContainerPort = container.ApiPort,
                            Name = container.ContainerPortName
                        }
                    },
                    Env = dockerImage.CreateEnvironmentVariables(offline, container)
                });
            }
            return result;
        }

        private void DeleteDeployment(Kubernetes client, OnlineCodexNodes online)
        {
            if (online.Deployment == null) return;
            client.DeleteNamespacedDeployment(online.Deployment.Name(), K8sNamespace);
            online.Deployment = null;
        }

        #endregion

        #region Namespace management

        private void EnsureTestNamespace(Kubernetes client)
        {
            if (IsTestNamespaceOnline(client)) return;

            var namespaceSpec = new V1Namespace
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = K8sNamespace,
                    Labels = new Dictionary<string, string> { { "name", K8sNamespace } }
                }
            };
            client.CreateNamespace(namespaceSpec);
        }

        private void DeleteNamespace(Kubernetes client)
        {
            if (IsTestNamespaceOnline(client))
            {
                client.DeleteNamespace(K8sNamespace, null, null, gracePeriodSeconds: 0);
            }
        }

        #endregion

        private static Kubernetes CreateClient()
        {
            // todo: If the default KubeConfig file does not suffice, change it here:
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            return new Kubernetes(config);
        }

        private static bool IsTestNamespaceOnline(Kubernetes client)
        {
            return client.ListNamespace().Items.Any(n => n.Metadata.Name == K8sNamespace);
        }

        private OnlineCodexNodes GetAndRemoveActiveNodeFor(IOnlineCodexNodes node)
        {
            var n = (OnlineCodexNodes)node;
            activeCodexNodes.Remove(n);
            return n;
        }
    }
}
