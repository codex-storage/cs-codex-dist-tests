using k8s;
using k8s.Models;

namespace CodexDistTests.TestCore
{
    public interface IK8sManager
    {
        IOnlineCodexNode BringOnline(OfflineCodexNode node);
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

            var activeNode = new ActiveNode(GetFreePort(), GetNodeOrderNumber());
            var codexNode = new OnlineCodexNode(node, fileManager, activeNode.Port);
            activeNodes.Add(codexNode, activeNode);

            CreateDeployment(activeNode, client, node);
            CreateService(activeNode, client);

            WaitUntilOnline(activeNode, client);

            return codexNode;
        }

        public IOfflineCodexNode BringOffline(IOnlineCodexNode node)
        {
            var client = CreateClient();

            var n = (OnlineCodexNode)node;
            var activeNode = activeNodes[n];
            activeNodes.Remove(n);

            var deploymentName = activeNode.Deployment.Name();
            BringOffline(activeNode, client);
            WaitUntilOffline(deploymentName, client);

            return n.Origin;
        }

        public void DeleteAllResources()
        {
            var client = CreateClient();

            foreach (var activeNode in activeNodes.Values)
            {
                BringOffline(activeNode, client);
            }

            DeleteNamespace(client);

            WaitUntilZeroPods(client);
            WaitUntilNamespaceDeleted(client);
        }

        private void BringOffline(ActiveNode activeNode, Kubernetes client)
        {
            DeleteDeployment(activeNode, client);
            DeleteService(activeNode, client);
        }

        #region Waiting

        private void WaitUntilOnline(ActiveNode activeNode, Kubernetes client)
        {
            while (activeNode.Deployment?.Status.AvailableReplicas == null || activeNode.Deployment.Status.AvailableReplicas != 1)
            {
                Timing.WaitForServiceDelay();
                activeNode.Deployment = client.ReadNamespacedDeployment(activeNode.Deployment.Name(), k8sNamespace);
            }
        }

        private void WaitUntilOffline(string deploymentName, Kubernetes client)
        {
            var deployment = client.ReadNamespacedDeployment(deploymentName, k8sNamespace);
            while (deployment != null && deployment.Status.AvailableReplicas > 0)
            {
                Timing.WaitForServiceDelay();
                deployment = client.ReadNamespacedDeployment(deploymentName, k8sNamespace);
            }
        }

        private void WaitUntilZeroPods(Kubernetes client)
        {
            var pods = client.ListNamespacedPod(k8sNamespace);
            while (pods.Items.Any())
            {
                Timing.WaitForServiceDelay();
                pods = client.ListNamespacedPod(k8sNamespace);
            }
        }

        private void WaitUntilNamespaceDeleted(Kubernetes client)
        {
            var namespaces = client.ListNamespace();
            while (namespaces.Items.Any(n => n.Metadata.Name == k8sNamespace))
            {
                Timing.WaitForServiceDelay();
                namespaces = client.ListNamespace();
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
        }

        #endregion

        private static Kubernetes CreateClient()
        {
            // todo: If the default KubeConfig file does not suffice, change it here:
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            return new Kubernetes(config);
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
            public ActiveNode(int port, int orderNumber)
            {
                SelectorName = orderNumber.ToString().PadLeft(6, '0');
                Port = port;
            }

            public string SelectorName { get; }
            public int Port { get; }
            public V1Deployment? Deployment { get; set; }
            public V1Service? Service { get; set; }

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
