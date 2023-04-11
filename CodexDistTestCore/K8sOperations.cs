using CodexDistTestCore.Config;
using CodexDistTestCore.Marketplace;
using CodexDistTestCore.Metrics;
using k8s;
using k8s.Models;
using NUnit.Framework;

namespace CodexDistTestCore
{
    public class K8sOperations
    {
        private readonly CodexDockerImage dockerImage = new CodexDockerImage();
        private readonly K8sCluster k8sCluster = new K8sCluster();
        private readonly Kubernetes client;
        private readonly KnownK8sPods knownPods;

        public K8sOperations(KnownK8sPods knownPods)
        {
            this.knownPods = knownPods;

            client = new Kubernetes(k8sCluster.GetK8sClientConfig());
        }

        public void Close()
        {
            client.Dispose();
        }

        public void BringOnline(CodexNodeGroup online, OfflineCodexNodes offline)
        {
            EnsureTestNamespace();

            CreateDeployment(online, offline);
            CreateService(online);

            WaitUntilOnline(online);
            FetchPodInfo(online);
        }

        public void BringOffline(CodexNodeGroup online)
        {
            var deploymentName = online.Deployment.Name();
            DeleteDeployment(online);
            DeleteService(online);
            WaitUntilOffline(deploymentName);
        }

        public void DeleteAllResources()
        {
            DeleteNamespace();

            WaitUntilZeroPods();
            WaitUntilNamespaceDeleted();
        }

        public void FetchPodLog(OnlineCodexNode node, IPodLogHandler logHandler)
        {
            var stream = client.ReadNamespacedPodLog(node.Group.PodInfo!.Name, K8sNamespace, node.Container.Name);
            logHandler.Log(stream);
        }

        public string ExecuteCommand(PodInfo pod, string containerName, string command, params string[] arguments)
        {
            var runner = new CommandRunner(client, pod, containerName, command, arguments);
            runner.Run();
            return runner.GetStdOut();
        }

        public PrometheusInfo BringOnlinePrometheus(K8sPrometheusSpecs spec)
        {
            EnsureTestNamespace();

            CreatePrometheusDeployment(spec);
            CreatePrometheusService(spec);
            WaitUntilPrometheusOnline(spec);

            return new PrometheusInfo(spec.ServicePort, FetchNewPod());
        }

        public PodInfo BringOnlineGethBootstrapNode(K8sGethBoostrapSpecs spec)
        {
            EnsureTestNamespace();

            CreateGethBootstrapDeployment(spec);
            CreateGethBootstrapService(spec);
            WaitUntilGethBootstrapOnline(spec);

            return FetchNewPod();
        }

        public PodInfo BringOnlineGethCompanionGroup(GethBootstrapInfo info, GethCompanionGroup group)
        {
            EnsureTestNamespace();

            CreateGethCompanionDeployment(info, group);
            WaitUntilGethCompanionGroupOnline(info.Spec, group);

            return FetchNewPod();
        }

        private void FetchPodInfo(CodexNodeGroup online)
        {
            online.PodInfo = FetchNewPod();
        }

        private PodInfo FetchNewPod()
        {
            var pods = client.ListNamespacedPod(K8sNamespace).Items;

            var newPods = pods.Where(p => !knownPods.Contains(p.Name())).ToArray();
            Assert.That(newPods.Length, Is.EqualTo(1), "Expected only 1 pod to be created. Test infra failure.");

            var newPod = newPods.Single();
            var info = new PodInfo(newPod.Name(), newPod.Status.PodIP);

            Assert.That(!string.IsNullOrEmpty(info.Name), "Invalid pod name received. Test infra failure.");
            Assert.That(!string.IsNullOrEmpty(info.Ip), "Invalid pod IP received. Test infra failure.");

            knownPods.Add(newPod.Name());
            return info;
        }

        #region Waiting

        private void WaitUntilOnline(CodexNodeGroup online)
        {
            WaitUntil(() =>
            {
                online.Deployment = client.ReadNamespacedDeployment(online.Deployment.Name(), K8sNamespace);
                return online.Deployment?.Status.AvailableReplicas != null && online.Deployment.Status.AvailableReplicas > 0;
            });
        }

        private void WaitUntilOffline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployment = client.ReadNamespacedDeployment(deploymentName, K8sNamespace);
                return deployment == null || deployment.Status.AvailableReplicas == 0;
            });
        }

        private void WaitUntilZeroPods()
        {
            WaitUntil(() => !client.ListNamespacedPod(K8sNamespace).Items.Any());
        }

        private void WaitUntilNamespaceDeleted()
        {
            WaitUntil(() => !IsTestNamespaceOnline());
        }

        private void WaitUntilPrometheusOnline(K8sPrometheusSpecs spec)
        {
            WaitUntilDeploymentOnline(spec.GetDeploymentName());
        }

        private void WaitUntilGethBootstrapOnline(K8sGethBoostrapSpecs spec)
        {
            WaitUntilDeploymentOnline(spec.GetBootstrapDeploymentName());
        }

        private void WaitUntilGethCompanionGroupOnline(K8sGethBoostrapSpecs spec, GethCompanionGroup group)
        {
            WaitUntilDeploymentOnline(spec.GetCompanionDeploymentName(group));
        }

        private void WaitUntilDeploymentOnline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployment = client.ReadNamespacedDeployment(deploymentName, K8sNamespace);
                return deployment?.Status.AvailableReplicas != null && deployment.Status.AvailableReplicas > 0;
            });
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

        private void CreateService(CodexNodeGroup online)
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

        private List<V1ServicePort> CreateServicePorts(CodexNodeGroup online)
        {
            var result = new List<V1ServicePort>();
            var containers = online.GetContainers();
            foreach (var container in containers)
            {
                result.Add(new V1ServicePort
                {
                    Name = container.ServicePortName,
                    Protocol = "TCP",
                    Port = container.ApiPort,
                    TargetPort = container.ContainerPortName,
                    NodePort = container.ServicePort
                });
            }
            return result;
        }

        private void DeleteService(CodexNodeGroup online)
        {
            if (online.Service == null) return;
            client.DeleteNamespacedService(online.Service.Name(), K8sNamespace);
            online.Service = null;
        }

        private void CreatePrometheusService(K8sPrometheusSpecs spec)
        {
            client.CreateNamespacedService(spec.CreatePrometheusService(), K8sNamespace);
        }

        private void CreateGethBootstrapService(K8sGethBoostrapSpecs spec)
        {
            client.CreateNamespacedService(spec.CreateGethBootstrapService(), K8sNamespace);
        }

        #endregion

        #region Deployment management

        private void CreateDeployment(CodexNodeGroup online, OfflineCodexNodes offline)
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
                            NodeSelector = CreateNodeSelector(offline),
                            Containers = CreateDeploymentContainers(online, offline)
                        }
                    }
                }
            };

            online.Deployment = client.CreateNamespacedDeployment(deploymentSpec, K8sNamespace);
        }

        private IDictionary<string, string> CreateNodeSelector(OfflineCodexNodes offline)
        {
            if (offline.Location == Location.Unspecified) return new Dictionary<string, string>();

            return new Dictionary<string, string> 
            { 
                { "codex-test-location", k8sCluster.GetNodeLabelForLocation(offline.Location) }
            };
        }

        private List<V1Container> CreateDeploymentContainers(CodexNodeGroup group, OfflineCodexNodes offline)
        {
            var result = new List<V1Container>();
            var containers = group.GetContainers();
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

        private void DeleteDeployment(CodexNodeGroup group)
        {
            if (group.Deployment == null) return;
            client.DeleteNamespacedDeployment(group.Deployment.Name(), K8sNamespace);
            group.Deployment = null;
        }

        private void CreatePrometheusDeployment(K8sPrometheusSpecs spec)
        {
            client.CreateNamespacedDeployment(spec.CreatePrometheusDeployment(), K8sNamespace);
        }

        private void CreateGethBootstrapDeployment(K8sGethBoostrapSpecs spec)
        {
            client.CreateNamespacedDeployment(spec.CreateGethBootstrapDeployment(), K8sNamespace);
        }

        private void CreateGethCompanionDeployment(GethBootstrapInfo info, GethCompanionGroup group)
        {
            client.CreateNamespacedDeployment(info.Spec.CreateGethCompanionDeployment(group, info), K8sNamespace);
        }

        #endregion

        #region Namespace management

        private void EnsureTestNamespace()
        {
            if (IsTestNamespaceOnline()) return;

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

        private void DeleteNamespace()
        {
            if (IsTestNamespaceOnline())
            {
                client.DeleteNamespace(K8sNamespace, null, null, gracePeriodSeconds: 0);
            }
        }

        private string K8sNamespace
        {
            get { return K8sCluster.K8sNamespace; }
        }

        #endregion

        private bool IsTestNamespaceOnline()
        {
            return client.ListNamespace().Items.Any(n => n.Metadata.Name == K8sNamespace);
        }

        private class CommandRunner
        {
            private readonly Kubernetes client;
            private readonly PodInfo pod;
            private readonly string containerName;
            private readonly string command;
            private readonly string[] arguments;
            private readonly List<string> lines = new List<string>();

            public CommandRunner(Kubernetes client, PodInfo pod, string containerName, string command, string[] arguments)
            {
                this.client = client;
                this.pod = pod;
                this.containerName = containerName;
                this.command = command;
                this.arguments = arguments;
            }

            public void Run()
            {
                var input = new[] { command }.Concat(arguments).ToArray();

                Utils.Wait(client.NamespacedPodExecAsync(
                    pod.Name, K8sCluster.K8sNamespace, containerName, input, false, Callback, new CancellationToken()));
            }

            public string GetStdOut()
            {
                return string.Join(Environment.NewLine, lines);
            }

            private Task Callback(Stream stdIn, Stream stdOut, Stream stdErr)
            {
                using var streamReader = new StreamReader(stdOut);
                var line = streamReader.ReadLine();
                while (line != null)
                {
                    lines.Add(line);
                    line = streamReader.ReadLine();
                }

                return Task.CompletedTask;
            }
        }
    }
}
