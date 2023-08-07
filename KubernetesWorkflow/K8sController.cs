using k8s;
using k8s.Models;
using Logging;
using Utils;

namespace KubernetesWorkflow
{
    public class K8sController
    {
        private readonly BaseLog log;
        private readonly K8sCluster cluster;
        private readonly KnownK8sPods knownPods;
        private readonly WorkflowNumberSource workflowNumberSource;
        private readonly K8sClient client;

        public K8sController(BaseLog log, K8sCluster cluster, KnownK8sPods knownPods, WorkflowNumberSource workflowNumberSource, string testNamespace)
        {
            this.log = log;
            this.cluster = cluster;
            this.knownPods = knownPods;
            this.workflowNumberSource = workflowNumberSource;
            client = new K8sClient(cluster.GetK8sClientConfig());

            K8sTestNamespace = cluster.Configuration.K8sNamespacePrefix + testNamespace;
        }

        public void Dispose()
        {
            client.Dispose();
        }
        
        public RunningPod BringOnline(ContainerRecipe[] containerRecipes, Location location)
        {
            log.Debug();
            DiscoverK8sNodes();
            EnsureTestNamespace();

            var deploymentName = CreateDeployment(containerRecipes, location);
            var (serviceName, servicePortsMap) = CreateService(containerRecipes);
            var podInfo = FetchNewPod();

            return new RunningPod(cluster, podInfo, deploymentName, serviceName, servicePortsMap.ToArray());
        }

        public void Stop(RunningPod pod)
        {
            log.Debug();
            if (!string.IsNullOrEmpty(pod.ServiceName)) DeleteService(pod.ServiceName);
            DeleteDeployment(pod.DeploymentName);
            WaitUntilDeploymentOffline(pod.DeploymentName);
            WaitUntilPodOffline(pod.PodInfo.Name);
        }

        public void DownloadPodLog(RunningPod pod, ContainerRecipe recipe, ILogHandler logHandler)
        {
            log.Debug();
            using var stream = client.Run(c => c.ReadNamespacedPodLog(pod.PodInfo.Name, K8sTestNamespace, recipe.Name));
            logHandler.Log(stream);
        }

        public string ExecuteCommand(RunningPod pod, string containerName, string command, params string[] args)
        {
            var cmdAndArgs = $"{containerName}: {command} ({string.Join(",", args)})";
            log.Debug(cmdAndArgs);

            var runner = new CommandRunner(client, K8sTestNamespace, pod, containerName, command, args);
            runner.Run();
            var result = runner.GetStdOut();

            log.Debug($"{cmdAndArgs} = '{result}'");
            return result;
        }

        public void DeleteAllResources()
        {
            log.Debug();

            var all = client.Run(c => c.ListNamespace().Items);
            var namespaces = all.Select(n => n.Name()).Where(n => n.StartsWith(cluster.Configuration.K8sNamespacePrefix));

            foreach (var ns in namespaces)
            {
                DeleteNamespace(ns);
            }
            foreach (var ns in namespaces)
            {
                WaitUntilNamespaceDeleted(ns);
            }
        }

        public void DeleteTestNamespace()
        {
            log.Debug();
            if (IsTestNamespaceOnline())
            {
                client.Run(c => c.DeleteNamespace(K8sTestNamespace, null, null, gracePeriodSeconds: 0));
            }
            WaitUntilNamespaceDeleted();
        }

        public void DeleteNamespace(string ns)
        {
            log.Debug();
            if (IsNamespaceOnline(ns))
            {
                client.Run(c => c.DeleteNamespace(ns, null, null, gracePeriodSeconds: 0));
            }
        }

        #region Discover K8s Nodes

        private void DiscoverK8sNodes()
        {
            if (cluster.AvailableK8sNodes == null || !cluster.AvailableK8sNodes.Any())
            {
                cluster.AvailableK8sNodes = GetAvailableK8sNodes();
                if (cluster.AvailableK8sNodes.Length < 3)
                {
                    log.Debug($"Warning: For full location support, at least 3 Kubernetes Nodes are required in the cluster. Nodes found: '{string.Join(",", cluster.AvailableK8sNodes.Select(p => $"{p.Key}={p.Value}"))}'.");
                }
            }
        }

        private K8sNodeLabel[] GetAvailableK8sNodes()
        {
            var nodes = client.Run(c => c.ListNode());

            var optionals = nodes.Items.Select(i => CreateNodeLabel(i));
            return optionals.Where(n => n != null).Select(n => n!).ToArray();
        }

        private K8sNodeLabel? CreateNodeLabel(V1Node i)
        {
            var keys = i.Metadata.Labels.Keys;
            var hostnameKey = keys.SingleOrDefault(k => k.ToLowerInvariant().Contains("hostname"));
            if (hostnameKey != null)
            {
                var hostnameValue = i.Metadata.Labels[hostnameKey];
                return new K8sNodeLabel(hostnameKey, hostnameValue);
            }
            return null;
        }

        #endregion

        #region Namespace management

        private string K8sTestNamespace { get; }

        private void EnsureTestNamespace()
        {
            if (IsTestNamespaceOnline()) return;

            var namespaceSpec = new V1Namespace
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = K8sTestNamespace,
                    Labels = new Dictionary<string, string> { { "name", K8sTestNamespace } }
                }
            };
            client.Run(c => c.CreateNamespace(namespaceSpec));
            WaitUntilNamespaceCreated();

            CreatePolicy();
        }

        private bool IsTestNamespaceOnline()
        {
            return IsNamespaceOnline(K8sTestNamespace);
        }

        private bool IsNamespaceOnline(string name)
        {
            return client.Run(c => c.ListNamespace().Items.Any(n => n.Metadata.Name == name));
        }

        private void CreatePolicy()
        {
            client.Run(c =>
            {
                var body = new V1NetworkPolicy
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = "isolate-policy",
                        NamespaceProperty = K8sTestNamespace
                    },
                    Spec = new V1NetworkPolicySpec
                    {
                        PodSelector = new V1LabelSelector {},
                        PolicyTypes = new[]
                        {
                            "Ingress",
                            "Egress"
                        },
                        Ingress = new List<V1NetworkPolicyIngressRule>
                        {
                            new V1NetworkPolicyIngressRule
                            {
                                FromProperty = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        PodSelector = new V1LabelSelector {}
                                    }
                                }
                            },
                            new V1NetworkPolicyIngressRule
                            {
                                FromProperty = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        NamespaceSelector = new V1LabelSelector
                                        {
                                            MatchLabels = GetRunnerNamespaceSelector()
                                        }
                                    }
                                }
                            }
                        },
                        Egress = new List<V1NetworkPolicyEgressRule>
                        {
                            new V1NetworkPolicyEgressRule
                            {
                                To = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        PodSelector = new V1LabelSelector {}
                                    }
                                }
                            },
                            new V1NetworkPolicyEgressRule
                            {
                                To = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        NamespaceSelector = new V1LabelSelector
                                        {
                                            MatchLabels = new Dictionary<string, string> { { "kubernetes.io/metadata.name", "kube-system" } }
                                        }
                                    },
                                    new V1NetworkPolicyPeer
                                    {
                                        PodSelector = new V1LabelSelector
                                        {
                                            MatchLabels = new Dictionary<string, string> { { "k8s-app", "kube-dns" } }
                                        }
                                    }
                                },
                                Ports = new List<V1NetworkPolicyPort>
                                {
                                    new V1NetworkPolicyPort
                                    {
                                        Port = new IntstrIntOrString
                                        {
                                            Value = "53"
                                        },
                                        Protocol = "UDP"
                                    }
                                }
                            },
                            new V1NetworkPolicyEgressRule
                            {
                                To = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        IpBlock = new V1IPBlock
                                        {
                                          Cidr = "0.0.0.0/0"
                                        }
                                    }
                                },
                                Ports = new List<V1NetworkPolicyPort>
                                {
                                    new V1NetworkPolicyPort
                                    {
                                        Port = new IntstrIntOrString
                                        {
                                            Value = "80"
                                        },
                                        Protocol = "TCP"
                                    },
                                    new V1NetworkPolicyPort
                                    {
                                        Port = new IntstrIntOrString
                                        {
                                            Value = "443"
                                        },
                                        Protocol = "TCP"
                                    }
                                }
                            }

                        }
                    }
                };

                c.CreateNamespacedNetworkPolicy(body, K8sTestNamespace);
            });
        }

        #endregion

        #region Deployment management

        private string CreateDeployment(ContainerRecipe[] containerRecipes, Location location)
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = CreateDeploymentMetadata(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = GetSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = GetSelector()
                        },
                        Spec = new V1PodSpec
                        {
                            NodeSelector = CreateNodeSelector(location),
                            Containers = CreateDeploymentContainers(containerRecipes)
                        }
                    }
                }
            };

            client.Run(c => c.CreateNamespacedDeployment(deploymentSpec, K8sTestNamespace));
            WaitUntilDeploymentOnline(deploymentSpec.Metadata.Name);

            return deploymentSpec.Metadata.Name;
        }

        private void DeleteDeployment(string deploymentName)
        {
            client.Run(c => c.DeleteNamespacedDeployment(deploymentName, K8sTestNamespace));
            WaitUntilDeploymentOffline(deploymentName);
        }

        private IDictionary<string, string> CreateNodeSelector(Location location)
        {
            var nodeLabel = cluster.GetNodeLabelForLocation(location);
            if (nodeLabel == null) return new Dictionary<string, string>();

            return new Dictionary<string, string>
            {
                { nodeLabel.Key, nodeLabel.Value }
            };
        }

        private IDictionary<string, string> GetSelector()
        {
            return new Dictionary<string, string> 
            { 
                { "codex-test-node", "dist-test-" + workflowNumberSource.WorkflowNumber }
                // tests-type=dist-tests
                // app=codex
                // runid=20230721-085043
            };
        }

        private IDictionary<string, string> GetRunnerNamespaceSelector()
        {
            return new Dictionary<string, string> { { "kubernetes.io/metadata.name", "default" } };
        }

        private V1ObjectMeta CreateDeploymentMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "deploy-" + workflowNumberSource.WorkflowNumber,
                NamespaceProperty = K8sTestNamespace,
                Labels = GetSelector()
            };
        }

        private List<V1Container> CreateDeploymentContainers(ContainerRecipe[] containerRecipes)
        {
            return containerRecipes.Select(r => CreateDeploymentContainer(r)).ToList();
        }

        private V1Container CreateDeploymentContainer(ContainerRecipe recipe)
        {
            return new V1Container
            {
                Name = recipe.Name,
                Image = recipe.Image,
                ImagePullPolicy = "Always",
                Ports = CreateContainerPorts(recipe),
                Env = CreateEnv(recipe)
            };
        }

        private List<V1EnvVar> CreateEnv(ContainerRecipe recipe)
        {
            return recipe.EnvVars.Select(CreateEnvVar).ToList();
        }

        private V1EnvVar CreateEnvVar(EnvVar envVar)
        {
            return new V1EnvVar
            {
                Name = envVar.Name,
                Value = envVar.Value,
            };
        }

        private List<V1ContainerPort> CreateContainerPorts(ContainerRecipe recipe)
        {
            var exposedPorts = recipe.ExposedPorts.Select(p => CreateContainerPort(recipe, p));
            var internalPorts = recipe.InternalPorts.Select(p => CreateContainerPort(recipe, p));
            return exposedPorts.Concat(internalPorts).ToList();
        }

        private V1ContainerPort CreateContainerPort(ContainerRecipe recipe, Port port)
        {
            return new V1ContainerPort
            {
                Name = GetNameForPort(recipe, port),
                ContainerPort = port.Number 
            };
        }

        private string GetNameForPort(ContainerRecipe recipe, Port port)
        {
            return $"p{workflowNumberSource.WorkflowNumber}-{recipe.Number}-{port.Number}";
        }

        #endregion

        #region Service management

        private (string, List<ContainerRecipePortMapEntry>) CreateService(ContainerRecipe[] containerRecipes)
        {
            var result = new List<ContainerRecipePortMapEntry>();

            var ports = CreateServicePorts(containerRecipes);

            if (!ports.Any())
            {
                // None of these container-recipes wish to expose anything via a service port.
                // So, we don't have to create a service.
                return (string.Empty, result);
            }

            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = CreateServiceMetadata(),
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = GetSelector(),
                    Ports = ports
                }
            };

            client.Run(c => c.CreateNamespacedService(serviceSpec, K8sTestNamespace));

            ReadBackServiceAndMapPorts(serviceSpec, containerRecipes, result);

            return (serviceSpec.Metadata.Name, result);
        }

        private void ReadBackServiceAndMapPorts(V1Service serviceSpec, ContainerRecipe[] containerRecipes, List<ContainerRecipePortMapEntry> result)
        {
            // For each container-recipe, we need to figure out which service-ports it was assigned by K8s.
            var readback = client.Run(c => c.ReadNamespacedService(serviceSpec.Metadata.Name, K8sTestNamespace));
            foreach (var r in containerRecipes)
            {
                if (r.ExposedPorts.Any())
                {
                    var firstExposedPort = r.ExposedPorts.First();
                    var portName = GetNameForPort(r, firstExposedPort);

                    var matchingServicePorts = readback.Spec.Ports.Where(p => p.Name == portName);
                    if (matchingServicePorts.Any())
                    {
                        // These service ports belongs to this recipe.
                        var optionals = matchingServicePorts.Select(p => MapNodePortIfAble(p, portName));
                        var ports = optionals.Where(p => p != null).Select(p => p!).ToArray();

                        result.Add(new ContainerRecipePortMapEntry(r.Number, ports));
                    }
                }
            }
        }

        private Port? MapNodePortIfAble(V1ServicePort p, string tag)
        {
            if (p.NodePort == null) return null;
            return new Port(p.NodePort.Value, tag);
        }

        private void DeleteService(string serviceName)
        {
            client.Run(c => c.DeleteNamespacedService(serviceName, K8sTestNamespace));
        }

        private V1ObjectMeta CreateServiceMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "service-" + workflowNumberSource.WorkflowNumber,
                NamespaceProperty = K8sTestNamespace
            };
        }

        private List<V1ServicePort> CreateServicePorts(ContainerRecipe[] recipes)
        {
            var result = new List<V1ServicePort>();
            foreach (var recipe in recipes)
            {
                result.AddRange(CreateServicePorts(recipe));
            }
            return result;
        }

        private List<V1ServicePort> CreateServicePorts(ContainerRecipe recipe)
        {
            var result = new List<V1ServicePort>();
            foreach (var port in recipe.ExposedPorts)
            {
                result.Add(new V1ServicePort
                {
                    Name = GetNameForPort(recipe, port),
                    Protocol = "TCP",
                    Port = port.Number,
                    TargetPort = GetNameForPort(recipe, port),
                });                
            }

            return result;
        }

        #endregion

        #region Waiting

        private void WaitUntilNamespaceCreated() 
        {
            WaitUntil(() => IsTestNamespaceOnline());
        }

        private void WaitUntilNamespaceDeleted()
        {
            WaitUntil(() => !IsTestNamespaceOnline());
        }

        private void WaitUntilNamespaceDeleted(string name)
        {
            WaitUntil(() => !IsNamespaceOnline(name));
        }

        private void WaitUntilDeploymentOnline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployment = client.Run(c => c.ReadNamespacedDeployment(deploymentName, K8sTestNamespace));
                return deployment?.Status.AvailableReplicas != null && deployment.Status.AvailableReplicas > 0;
            });
        }

        private void WaitUntilDeploymentOffline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployments = client.Run(c => c.ListNamespacedDeployment(K8sTestNamespace));
                var deployment = deployments.Items.SingleOrDefault(d => d.Metadata.Name == deploymentName);
                return deployment == null || deployment.Status.AvailableReplicas == 0;
            });
        }

        private void WaitUntilPodOffline(string podName)
        {
            WaitUntil(() =>
            {
                var pods = client.Run(c => c.ListNamespacedPod(K8sTestNamespace)).Items;
                var pod = pods.SingleOrDefault(p => p.Metadata.Name == podName);
                return pod == null;
            });
        }

        private void WaitUntil(Func<bool> predicate)
        {
            var sw = Stopwatch.Begin(log, true);
            try
            {
                Time.WaitUntil(predicate, cluster.K8sOperationTimeout(), cluster.WaitForK8sServiceDelay());
            }
            finally
            {
                sw.End("", 1);
            }
        }

        #endregion

        private PodInfo FetchNewPod()
        {
            var pods = client.Run(c => c.ListNamespacedPod(K8sTestNamespace)).Items;

            var newPods = pods.Where(p => !knownPods.Contains(p.Name())).ToArray();
            if (newPods.Length != 1) throw new InvalidOperationException("Expected only 1 pod to be created. Test infra failure.");

            var newPod = newPods.Single();
            var name = newPod.Name();
            var ip = newPod.Status.PodIP;
            var k8sNodeName = newPod.Spec.NodeName;

            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Invalid pod name received. Test infra failure.");
            if (string.IsNullOrEmpty(ip)) throw new InvalidOperationException("Invalid pod IP received. Test infra failure.");

            knownPods.Add(name);
            return new PodInfo(name, ip, k8sNodeName);
        }
    }
}
