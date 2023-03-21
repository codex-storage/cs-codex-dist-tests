using k8s;
using k8s.Models;
using NUnit.Framework;

namespace CodexDistTests.TestCore
{
    public interface IK8sManager
    {
        IOnlineCodexNode BringOnline(OfflineCodexNode node);
        IOfflineCodexNode BringOffline(IOnlineCodexNode node);
    }

    public class K8sManager : IK8sManager
    {
        public const string K8sNamespace = "codex-test-namespace";
        private readonly CodexDockerImage dockerImage = new CodexDockerImage();
        private readonly NumberSource numberSource = new NumberSource();
        private readonly Dictionary<OnlineCodexNode, ActiveNode> activeNodes = new Dictionary<OnlineCodexNode, ActiveNode>();
        private readonly List<string> knownActivePodNames = new List<string>();
        private readonly IFileManager fileManager;

        public K8sManager(IFileManager fileManager)
        {
            this.fileManager = fileManager;
        }

        public IOnlineCodexNode BringOnline(OfflineCodexNode node)
        {
            var client = CreateClient();

            EnsureTestNamespace(client);

            var activeNode = new ActiveNode(node, numberSource.GetFreePort(), numberSource.GetNodeOrderNumber());
            var codexNode = new OnlineCodexNode(this, fileManager, activeNode.Port);
            activeNodes.Add(codexNode, activeNode);

            CreateDeployment(activeNode, client, node);
            CreateService(activeNode, client);

            WaitUntilOnline(activeNode, client);
            TestLog.Log($"{activeNode.Describe()} online.");

            return codexNode;
        }

        public IOfflineCodexNode BringOffline(IOnlineCodexNode node)
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

        public void FetchAllPodsLogs(Action<string, string, Stream> onLog)
        {
            var client = CreateClient();
            foreach (var node in activeNodes.Values)
            {
                var nodeDescription = node.Describe();
                foreach (var podName in node.ActivePodNames)
                {
                    var stream = client.ReadNamespacedPodLog(podName, K8sNamespace);
                    onLog(node.SelectorName, $"{nodeDescription}:{podName}", stream);
                }
            }
        }

        private void BringOffline(ActiveNode activeNode, Kubernetes client)
        {
            DeleteDeployment(activeNode, client);
            DeleteService(activeNode, client);
        }

        #region Waiting

        private void WaitUntilOnline(ActiveNode activeNode, Kubernetes client)
        {
            WaitUntil(() =>
            {
                activeNode.Deployment = client.ReadNamespacedDeployment(activeNode.Deployment.Name(), K8sNamespace);
                return activeNode.Deployment?.Status.AvailableReplicas != null && activeNode.Deployment.Status.AvailableReplicas > 0;
            });

            AssignActivePodNames(activeNode, client);
        }

        private void AssignActivePodNames(ActiveNode activeNode, Kubernetes client)
        {
            var pods = client.ListNamespacedPod(K8sNamespace);
            var podNames = pods.Items.Select(p => p.Name());
            foreach (var podName in podNames)
            {
                if (!knownActivePodNames.Contains(podName))
                {
                    knownActivePodNames.Add(podName);
                    activeNode.ActivePodNames.Add(podName);
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

        private void CreateService(ActiveNode node, Kubernetes client)
        {
            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = node.GetServiceMetadata(),
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = node.GetSelector(),
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort
                        {
                            Protocol = "TCP",
                            Port = 8080,
                            TargetPort = node.GetContainerPortName(),
                            NodePort = node.Port
                        }
                    }
                }
            };

            node.Service = client.CreateNamespacedService(serviceSpec, K8sNamespace);
        }

        private void DeleteService(ActiveNode node, Kubernetes client)
        {
            if (node.Service == null) return;
            client.DeleteNamespacedService(node.Service.Name(), K8sNamespace);
            node.Service = null;
        }

        #endregion

        #region Deployment management

        private void CreateDeployment(ActiveNode node, Kubernetes client, OfflineCodexNode codexNode)
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = node.GetDeploymentMetadata(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = node.GetSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = node.GetSelector()
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = node.GetContainerName(),
                                    Image = dockerImage.GetImageTag(),
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = 8080,
                                            Name = node.GetContainerPortName()
                                        }
                                    },
                                    Env = dockerImage.CreateEnvironmentVariables(codexNode)
                                }
                            }
                        }
                    }
                }
            };

            node.Deployment = client.CreateNamespacedDeployment(deploymentSpec, K8sNamespace);
        }

        private void DeleteDeployment(ActiveNode node, Kubernetes client)
        {
            if (node.Deployment == null) return;
            client.DeleteNamespacedDeployment(node.Deployment.Name(), K8sNamespace);
            node.Deployment = null;
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

        private ActiveNode GetAndRemoveActiveNodeFor(IOnlineCodexNode node)
        {
            var n = (OnlineCodexNode)node;
            var activeNode = activeNodes[n];
            activeNodes.Remove(n);
            return activeNode;
        }
    }
}
