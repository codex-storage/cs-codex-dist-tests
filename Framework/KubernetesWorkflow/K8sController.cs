using k8s;
using k8s.Models;
using Logging;
using Utils;

namespace KubernetesWorkflow
{
    public class K8sController
    {
        private readonly ILog log;
        private readonly K8sCluster cluster;
        private readonly WorkflowNumberSource workflowNumberSource;
        private readonly K8sClient client;
        private const string podLabelKey = "pod-uuid";

        public K8sController(ILog log, K8sCluster cluster, WorkflowNumberSource workflowNumberSource, string k8sNamespace)
        {
            this.log = log;
            this.cluster = cluster;
            this.workflowNumberSource = workflowNumberSource;
            client = new K8sClient(cluster.GetK8sClientConfig());

            K8sNamespace = k8sNamespace;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public RunningPod BringOnline(ContainerRecipe[] containerRecipes, ILocation location)
        {
            log.Debug();
            EnsureNamespace();

            var podLabel = K8sNameUtils.Format(Guid.NewGuid().ToString());
            var deploymentName = CreateDeployment(containerRecipes, location, podLabel);
            var (serviceName, servicePortsMap) = CreateService(containerRecipes);

            var pods = client.Run(c => c.ListNamespacedPod(K8sNamespace));
            var pod = pods.Items.Single(p => p.Labels().Any(l => l.Key == podLabelKey && l.Value == podLabel));
            var podInfo = CreatePodInfo(pod);

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

        public void DownloadPodLog(RunningPod pod, ContainerRecipe recipe, ILogHandler logHandler, int? tailLines)
        {
            log.Debug();
            using var stream = client.Run(c => c.ReadNamespacedPodLog(pod.PodInfo.Name, K8sNamespace, recipe.Name, tailLines: tailLines));
            logHandler.Log(stream);
        }

        public string ExecuteCommand(RunningPod pod, string containerName, string command, params string[] args)
        {
            var cmdAndArgs = $"{containerName}: {command} ({string.Join(",", args)})";
            log.Debug(cmdAndArgs);

            var runner = new CommandRunner(client, K8sNamespace, pod, containerName, command, args);
            runner.Run();
            var result = runner.GetStdOut();

            log.Debug($"{cmdAndArgs} = '{result}'");
            return result;
        }

        public void DeleteAllNamespacesStartingWith(string prefix)
        {
            log.Debug();

            var all = client.Run(c => c.ListNamespace().Items);
            var namespaces = all.Select(n => n.Name()).Where(n => n.StartsWith(prefix));

            foreach (var ns in namespaces)
            {
                DeleteNamespace(ns);
            }
        }

        public void DeleteNamespace()
        {
            log.Debug();
            if (IsNamespaceOnline(K8sNamespace))
            {
                client.Run(c => c.DeleteNamespace(K8sNamespace, null, null, gracePeriodSeconds: 0));
            }
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

        public K8sNodeLabel[] GetAvailableK8sNodes()
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

        private string K8sNamespace { get; }

        private void EnsureNamespace()
        {
            if (IsNamespaceOnline(K8sNamespace)) return;

            var namespaceSpec = new V1Namespace
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = K8sNamespace,
                    Labels = new Dictionary<string, string> { { "name", K8sNamespace } }
                }
            };
            client.Run(c => c.CreateNamespace(namespaceSpec));
            WaitUntilNamespaceCreated();

            CreatePolicy();
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
                        NamespaceProperty = K8sNamespace
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
                            },
                            new V1NetworkPolicyIngressRule
                            {
                                FromProperty = new List<V1NetworkPolicyPeer>
                                {
                                    new V1NetworkPolicyPeer
                                    {
                                        NamespaceSelector = new V1LabelSelector
                                        {
                                            MatchLabels = GetPrometheusNamespaceSelector()
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

                c.CreateNamespacedNetworkPolicy(body, K8sNamespace);
            });
        }

        #endregion

        #region Deployment management

        private string CreateDeployment(ContainerRecipe[] containerRecipes, ILocation location, string podLabel)
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = CreateDeploymentMetadata(containerRecipes),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = GetSelector(containerRecipes)
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = GetSelector(containerRecipes, podLabel),
                            Annotations = GetAnnotations(containerRecipes)
                        },
                        Spec = new V1PodSpec
                        {
                            NodeSelector = CreateNodeSelector(location),
                            Containers = CreateDeploymentContainers(containerRecipes),
                            Volumes = CreateVolumes(containerRecipes)
                        }
                    }
                }
            };

            client.Run(c => c.CreateNamespacedDeployment(deploymentSpec, K8sNamespace));
            WaitUntilDeploymentOnline(deploymentSpec.Metadata.Name);

            return deploymentSpec.Metadata.Name;
        }

        private void DeleteDeployment(string deploymentName)
        {
            client.Run(c => c.DeleteNamespacedDeployment(deploymentName, K8sNamespace));
            WaitUntilDeploymentOffline(deploymentName);
        }

        private IDictionary<string, string> CreateNodeSelector(ILocation location)
        {
            var nodeLabel = GetNodeLabelForLocation(location);
            if (nodeLabel == null) return new Dictionary<string, string>();

            return new Dictionary<string, string>
            {
                { nodeLabel.Key, nodeLabel.Value }
            };
        }

        private K8sNodeLabel? GetNodeLabelForLocation(ILocation location)
        {
            var l = (Location)location;
            return l.NodeLabel;
        }

        private IDictionary<string, string> GetSelector(ContainerRecipe[] containerRecipes)
        {
            return containerRecipes.First().PodLabels.GetLabels();
        }

        private IDictionary<string, string> GetSelector(ContainerRecipe[] containerRecipes, string podLabel)
        {
            var labels = containerRecipes.First().PodLabels.Clone();
            labels.Add(podLabelKey, podLabel);
            return labels.GetLabels();
        }

        private IDictionary<string, string> GetRunnerNamespaceSelector()
        {
            return new Dictionary<string, string> { { "kubernetes.io/metadata.name", "default" } };
        }

        private IDictionary<string, string> GetPrometheusNamespaceSelector()
        {
            return new Dictionary<string, string> { { "kubernetes.io/metadata.name", "monitoring" } };
        }

        private IDictionary<string, string> GetAnnotations(ContainerRecipe[] containerRecipes)
        {
            return containerRecipes.First().PodAnnotations.GetAnnotations();
        }

        private V1ObjectMeta CreateDeploymentMetadata(ContainerRecipe[] containerRecipes)
        {
            return new V1ObjectMeta
            {
                Name = string.Join('-',containerRecipes.Select(r => r.Name)),
                NamespaceProperty = K8sNamespace,
                Labels = GetSelector(containerRecipes),
                Annotations = GetAnnotations(containerRecipes)
            };
        }

        private List<V1Container> CreateDeploymentContainers(ContainerRecipe[] containerRecipes)
        {
            return containerRecipes.Select(CreateDeploymentContainer).ToList();
        }

        private V1Container CreateDeploymentContainer(ContainerRecipe recipe)
        {
            return new V1Container
            {
                Name = recipe.Name,
                Image = recipe.Image,
                ImagePullPolicy = "Always",
                Ports = CreateContainerPorts(recipe),
                Env = CreateEnv(recipe),
                VolumeMounts = CreateContainerVolumeMounts(recipe),
                Resources = CreateResourceLimits(recipe)
            };
        }

        private V1ResourceRequirements CreateResourceLimits(ContainerRecipe recipe)
        {
            return new V1ResourceRequirements
            {
                Requests = CreateResourceQuantities(recipe.Resources.Requests),
                Limits = CreateResourceQuantities(recipe.Resources.Limits)
            };
        }

        private Dictionary<string, ResourceQuantity> CreateResourceQuantities(ContainerResourceSet set)
        {
            var result = new Dictionary<string, ResourceQuantity>();
            if (set.MilliCPUs != 0)
            {
                result.Add("cpu", new ResourceQuantity($"{set.MilliCPUs}m"));
            }
            if (set.Memory.SizeInBytes != 0)
            {
                result.Add("memory", new ResourceQuantity(set.Memory.ToSuffixNotation()));
            }
            return result;
        }

        private List<V1VolumeMount> CreateContainerVolumeMounts(ContainerRecipe recipe)
        {
            return recipe.Volumes.Select(CreateContainerVolumeMount).ToList();
        }

        private V1VolumeMount CreateContainerVolumeMount(VolumeMount v)
        {
            return new V1VolumeMount
            {
                Name = v.VolumeName,
                MountPath = v.MountPath,
                SubPath = v.SubPath,
            };
        }

        private List<V1Volume> CreateVolumes(ContainerRecipe[] containerRecipes)
        {
            return containerRecipes.Where(c => c.Volumes.Any()).SelectMany(CreateVolumes).ToList();
        }

        private List<V1Volume> CreateVolumes(ContainerRecipe recipe)
        {
            return recipe.Volumes.Select(CreateVolume).ToList();
        }

        private V1Volume CreateVolume(VolumeMount v)
        {
            client.Run(c => c.CreateNamespacedPersistentVolumeClaim(new V1PersistentVolumeClaim
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = v.VolumeName,
                },
                Spec = new V1PersistentVolumeClaimSpec
                {
                    
                    AccessModes = new List<string>
                    {
                        "ReadWriteOnce"
                    },
                    Resources = CreateVolumeResourceRequirements(v),
                },
            }, K8sNamespace));

            if (!string.IsNullOrEmpty(v.HostPath))
            {
                return new V1Volume
                {
                    Name = v.VolumeName,
                    HostPath = new V1HostPathVolumeSource
                    {
                        Path = v.HostPath
                    }
                };
            }

            if (!string.IsNullOrEmpty(v.Secret))
            {
                return new V1Volume
                {
                    Name = v.VolumeName,
                    Secret = CreateVolumeSecret(v)
                };
            }

            return new V1Volume
            {
                Name = v.VolumeName,
                PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                {
                    ClaimName = v.VolumeName
                }
            };
        }

        private V1SecretVolumeSource CreateVolumeSecret(VolumeMount v)
        {
            if (string.IsNullOrWhiteSpace(v.Secret)) return null!;
            return new V1SecretVolumeSource
            {
                SecretName = v.Secret
            };
        }

        private V1ResourceRequirements CreateVolumeResourceRequirements(VolumeMount v)
        {
            if (v.ResourceQuantity == null) return null!;
            return new V1ResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity>()
                {
                    {"storage", new ResourceQuantity(v.ResourceQuantity) }
                }
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
            var exposedPorts = recipe.ExposedPorts.SelectMany(p => CreateContainerPort(recipe, p));
            var internalPorts = recipe.InternalPorts.SelectMany(p => CreateContainerPort(recipe, p));
            return exposedPorts.Concat(internalPorts).ToList();
        }

        private List<V1ContainerPort> CreateContainerPort(ContainerRecipe recipe, Port port)
        {
            var result = new List<V1ContainerPort>();
            if (port.IsTcp()) CreateTcpContainerPort(result, recipe, port);
            if (port.IsUdp()) CreateUdpContainerPort(result, recipe, port);
            return result;
        }

        private void CreateUdpContainerPort(List<V1ContainerPort> result, ContainerRecipe recipe, Port port)
        {
            result.Add(CreateContainerPort(recipe, port, "UDP"));
        }

        private void CreateTcpContainerPort(List<V1ContainerPort> result, ContainerRecipe recipe, Port port)
        {
            result.Add(CreateContainerPort(recipe, port, "TCP"));
        }

        private V1ContainerPort CreateContainerPort(ContainerRecipe recipe, Port port, string protocol)
        {
            return new V1ContainerPort
            {
                Name = GetNameForPort(recipe, port),
                ContainerPort = port.Number,
                Protocol = protocol
            };
        }

        private string GetNameForPort(ContainerRecipe recipe, Port port)
        {
            return $"p{workflowNumberSource.WorkflowNumber}-{recipe.Number}-{port.Number}-{port.Protocol.ToString().ToLowerInvariant()}";
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
                    Selector = GetSelector(containerRecipes),
                    Ports = ports
                }
            };

            client.Run(c => c.CreateNamespacedService(serviceSpec, K8sNamespace));

            ReadBackServiceAndMapPorts(serviceSpec, containerRecipes, result);

            return (serviceSpec.Metadata.Name, result);
        }

        private void ReadBackServiceAndMapPorts(V1Service serviceSpec, ContainerRecipe[] containerRecipes, List<ContainerRecipePortMapEntry> result)
        {
            // For each container-recipe, we need to figure out which service-ports it was assigned by K8s.
            var readback = client.Run(c => c.ReadNamespacedService(serviceSpec.Metadata.Name, K8sNamespace));
            foreach (var r in containerRecipes)
            {
                foreach (var port in r.ExposedPorts)
                {
                    var portName = GetNameForPort(r, port);

                    var matchingServicePorts = readback.Spec.Ports.Where(p => p.Name == portName);
                    if (matchingServicePorts.Any())
                    {
                        // These service ports belongs to this recipe.
                        var optionals = matchingServicePorts.Select(p => MapNodePortIfAble(p, port.Tag, port.Protocol));
                        var ports = optionals.Where(p => p != null).Select(p => p!).ToArray();

                        result.Add(new ContainerRecipePortMapEntry(r.Number, ports));
                    }
                }
            }
        }

        private Port? MapNodePortIfAble(V1ServicePort p, string tag, PortProtocol protocol)
        {
            if (p.NodePort == null) return null;
            return new Port(p.NodePort.Value, tag, protocol);
        }

        private void DeleteService(string serviceName)
        {
            client.Run(c => c.DeleteNamespacedService(serviceName, K8sNamespace));
        }

        private V1ObjectMeta CreateServiceMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "service-" + workflowNumberSource.WorkflowNumber,
                NamespaceProperty = K8sNamespace
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
                if (port.IsTcp()) CreateServicePort(result, recipe, port, "TCP");
                if (port.IsUdp()) CreateServicePort(result, recipe, port, "UDP");
            }
            return result;
        }

        private void CreateServicePort(List<V1ServicePort> result, ContainerRecipe recipe, Port port, string protocol)
        {
            result.Add(new V1ServicePort
            {
                Name = GetNameForPort(recipe, port),
                Protocol = protocol,
                Port = port.Number,
                TargetPort = GetNameForPort(recipe, port),
            });
        }

        #endregion

        #region Waiting

        private void WaitUntilNamespaceCreated() 
        {
            WaitUntil(() => IsNamespaceOnline(K8sNamespace));
        }

        private void WaitUntilDeploymentOnline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployment = client.Run(c => c.ReadNamespacedDeployment(deploymentName, K8sNamespace));
                return deployment?.Status.AvailableReplicas != null && deployment.Status.AvailableReplicas > 0;
            });
        }

        private void WaitUntilDeploymentOffline(string deploymentName)
        {
            WaitUntil(() =>
            {
                var deployments = client.Run(c => c.ListNamespacedDeployment(K8sNamespace));
                var deployment = deployments.Items.SingleOrDefault(d => d.Metadata.Name == deploymentName);
                return deployment == null || deployment.Status.AvailableReplicas == 0;
            });
        }

        private void WaitUntilPodOffline(string podName)
        {
            WaitUntil(() =>
            {
                var pods = client.Run(c => c.ListNamespacedPod(K8sNamespace)).Items;
                var pod = pods.SingleOrDefault(p => p.Metadata.Name == podName);
                return pod == null;
            });
        }

        private void WaitUntil(Func<bool> predicate)
        {
            var sw = Stopwatch.Begin(log, true);
            try
            {
                Time.WaitUntil(predicate, cluster.K8sOperationTimeout(), cluster.K8sOperationRetryDelay());
            }
            finally
            {
                sw.End("", 1);
            }
        }

        #endregion

        public CrashWatcher CreateCrashWatcher(RunningContainer container)
        {
            return new CrashWatcher(log, cluster.GetK8sClientConfig(), K8sNamespace, container);
        }

        private PodInfo CreatePodInfo(V1Pod pod)
        {
            var name = pod.Name();
            var ip = pod.Status.PodIP;
            var k8sNodeName = pod.Spec.NodeName;

            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Invalid pod name received. Test infra failure.");
            if (string.IsNullOrEmpty(ip)) throw new InvalidOperationException("Invalid pod IP received. Test infra failure.");

            return new PodInfo(name, ip, k8sNodeName);
        }
    }
}
