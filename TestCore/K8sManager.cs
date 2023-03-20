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
        private const string k8sNamespace = "codex-test-namespace";
        private readonly CodexDockerImage dockerImage = new CodexDockerImage();
        private readonly IFileManager fileManager;
        private int freePort;
        private int nodeOrderNumber;

        private V1Namespace? activeNamespace;
        private readonly Dictionary<OnlineCodexNode, ActiveNode> activeNodes = new Dictionary<OnlineCodexNode, ActiveNode>();
        private readonly List<string> knownActivePodNames = new List<string>();

        public K8sManager(IFileManager fileManager)
        {
            this.fileManager = fileManager;
            freePort = 30001;
            nodeOrderNumber = 0;
        }

        public IOnlineCodexNode BringOnline(OfflineCodexNode node)
        {
            var client = CreateClient();

            EnsureTestNamespace(client);

            var activeNode = new ActiveNode(node, GetFreePort(), GetNodeOrderNumber());
            var codexNode = new OnlineCodexNode(this, fileManager, activeNode.Port);
            activeNodes.Add(codexNode, activeNode);

            CreateDeployment(activeNode, client, node);
            CreateService(activeNode, client);

            WaitUntilOnline(activeNode, client);

            return codexNode;
        }

        public IOfflineCodexNode BringOffline(IOnlineCodexNode node)
        {
            var client = CreateClient();

            var activeNode = GetAndRemoveActiveNodeFor(node);

            var deploymentName = activeNode.Deployment.Name();
            BringOffline(activeNode, client);
            WaitUntilOffline(deploymentName, client);

            return activeNode.Origin;
        }

        public void DeleteAllResources()
        {
            var client = CreateClient();

            DeleteNamespace(client);

            WaitUntilZeroPods(client);
            WaitUntilNamespaceDeleted(client);
        }

        private void BringOffline(ActiveNode activeNode, Kubernetes client)
        {
            DownloadCodexNodeLog(activeNode, client);
            DeleteDeployment(activeNode, client);
            DeleteService(activeNode, client);
        }

        #region Waiting

        private void WaitUntilOnline(ActiveNode activeNode, Kubernetes client)
        {
            WaitUntil(() =>
            {
                activeNode.Deployment = client.ReadNamespacedDeployment(activeNode.Deployment.Name(), k8sNamespace);
                return activeNode.Deployment?.Status.AvailableReplicas != null && activeNode.Deployment.Status.AvailableReplicas > 0;
            });

            AssignActivePodNames(activeNode, client);
        }

        private void AssignActivePodNames(ActiveNode activeNode, Kubernetes client)
        {
            var pods = client.ListNamespacedPod(k8sNamespace);
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
                var deployment = client.ReadNamespacedDeployment(deploymentName, k8sNamespace);
                return deployment == null || deployment.Status.AvailableReplicas == 0;
            });
        }

        private void WaitUntilZeroPods(Kubernetes client)
        {
            WaitUntil(() => !client.ListNamespacedPod(k8sNamespace).Items.Any());
        }

        private void WaitUntilNamespaceDeleted(Kubernetes client)
        {
            WaitUntil(() => client.ListNamespace().Items.All(n => n.Metadata.Name != k8sNamespace));
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

            node.Service = client.CreateNamespacedService(serviceSpec, k8sNamespace);
        }

        private void DeleteService(ActiveNode node, Kubernetes client)
        {
            if (node.Service == null) return;
            client.DeleteNamespacedService(node.Service.Name(), k8sNamespace);
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

            node.Deployment = client.CreateNamespacedDeployment(deploymentSpec, k8sNamespace);
        }

        private void DeleteDeployment(ActiveNode node, Kubernetes client)
        {
            if (node.Deployment == null) return;
            client.DeleteNamespacedDeployment(node.Deployment.Name(), k8sNamespace);
            node.Deployment = null;
        }

        #endregion

        #region Namespace management

        private void EnsureTestNamespace(Kubernetes client)
        {
            if (activeNamespace != null) return;

            var namespaceSpec = new V1Namespace
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = k8sNamespace,
                    Labels = new Dictionary<string, string> { { "name", k8sNamespace } }
                }
            };
            activeNamespace = client.CreateNamespace(namespaceSpec);
        }

        private void DeleteNamespace(Kubernetes client)
        {
            if (activeNamespace == null) return;
            client.DeleteNamespace(activeNamespace.Name());
            activeNamespace = null;
        }

        #endregion

        private static Kubernetes CreateClient()
        {
            // todo: If the default KubeConfig file does not suffice, change it here:
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            return new Kubernetes(config);
        }

        private void DownloadCodexNodeLog(ActiveNode node, Kubernetes client)
        {
            //var client = CreateClient();
            var i = 0;
            foreach (var podName in node.ActivePodNames)
            {
                var stream = client.ReadNamespacedPodLog(podName, k8sNamespace);
                using (var fileStream = File.Create(node.SelectorName + i.ToString() + ".txt"))
                {
                    stream.CopyTo(fileStream);
                }
                i++;
            }
        }

        private ActiveNode GetAndRemoveActiveNodeFor(IOnlineCodexNode node)
        {
            var n = (OnlineCodexNode)node;
            var activeNode = activeNodes[n];
            activeNodes.Remove(n);
            return activeNode;
        }

        private int GetFreePort()
        {
            var port = freePort;
            freePort++;
            return port;
        }

        private int GetNodeOrderNumber()
        {
            var number = nodeOrderNumber;
            nodeOrderNumber++;
            return number;
        }

        public class ActiveNode
        {
            public ActiveNode(OfflineCodexNode origin, int port, int orderNumber)
            {
                Origin = origin;
                SelectorName = orderNumber.ToString().PadLeft(6, '0');
                Port = port;
            }

            public OfflineCodexNode Origin { get; }
            public string SelectorName { get; }
            public int Port { get; }
            public V1Deployment? Deployment { get; set; }
            public V1Service? Service { get; set; }
            public List<string> ActivePodNames { get; } = new List<string>();

            public V1ObjectMeta GetServiceMetadata()
            {
                return new V1ObjectMeta
                {
                    Name = "codex-test-entrypoint-" + SelectorName,
                    NamespaceProperty = k8sNamespace
                };
            }

            public V1ObjectMeta GetDeploymentMetadata()
            {
                return new V1ObjectMeta
                {
                    Name = "codex-test-node-" + SelectorName,
                    NamespaceProperty = k8sNamespace
                };
            }

            public Dictionary<string, string> GetSelector()
            {
                return new Dictionary<string, string> { { "codex-test-node", "dist-test-" + SelectorName } };
            }

            public string GetContainerPortName()
            {
                //Caution, was: "codex-api-port" + SelectorName
                //but string length causes 'UnprocessableEntity' exception in k8s.
                return "api-" + SelectorName;
            }

            public string GetContainerName()
            {
                return "codex-test-node";
            }
        }
    }
}
