using k8s;
using k8s.Models;

namespace KubernetesWorkflow
{
    public class K8sController
    {
        private readonly K8sCluster cluster;
        private readonly KnownK8sPods knownPods;
        private readonly WorkflowNumberSource workflowNumberSource;
        private readonly Kubernetes client;

        public K8sController(K8sCluster cluster, KnownK8sPods knownPods, WorkflowNumberSource workflowNumberSource)
        {
            this.cluster = cluster;
            this.knownPods = knownPods;
            this.workflowNumberSource = workflowNumberSource;

            client = new Kubernetes(cluster.GetK8sClientConfig());
        }

        public void Dispose()
        {
            client.Dispose();
        }
        
        public RunningPod BringOnline(ContainerRecipe[] containerRecipes, Location location)
        {
            EnsureTestNamespace();

            CreateDeployment(containerRecipes, location);
            var servicePortsMap = CreateService(containerRecipes);
            var (podName, podIp) = FetchNewPod();

            return new RunningPod(cluster, podName, podIp, servicePortsMap);
        }

        public void DeleteAllResources()
        {
            DeleteNamespace();

            WaitUntilNamespaceDeleted();
        }

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
            WaitUntilNamespaceCreated();
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
            get { return cluster.Configuration.K8sNamespace; }
        }

        private bool IsTestNamespaceOnline()
        {
            return client.ListNamespace().Items.Any(n => n.Metadata.Name == K8sNamespace);
        }

        #endregion

        #region Deployment management

        private void CreateDeployment(ContainerRecipe[] containerRecipes, Location location)
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

            client.CreateNamespacedDeployment(deploymentSpec, K8sNamespace);
            WaitUntilDeploymentCreated(deploymentSpec);
        }

        private IDictionary<string, string> CreateNodeSelector(Location location)
        {
            if (location == Location.Unspecified) return new Dictionary<string, string>();

            return new Dictionary<string, string>
            {
                { "codex-test-location", cluster.GetNodeLabelForLocation(location) }
            };
        }

        private IDictionary<string, string> GetSelector()
        {
            return new Dictionary<string, string> { { "codex-test-node", "dist-test-" + workflowNumberSource.WorkflowNumber } };
        }

        private V1ObjectMeta CreateDeploymentMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "deploy-" + workflowNumberSource.WorkflowNumber,
                NamespaceProperty = K8sNamespace
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

        private Dictionary<ContainerRecipe, Port[]> CreateService(ContainerRecipe[] containerRecipes)
        {
            var result = new Dictionary<ContainerRecipe, Port[]>();

            var ports = CreateServicePorts(result, containerRecipes);

            if (!ports.Any())
            {
                // None of these container-recipes wish to expose anything via a serice port.
                // So, we don't have to create a service.
                return result;
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

            client.CreateNamespacedService(serviceSpec, K8sNamespace);
            return result;
        }

        private V1ObjectMeta CreateServiceMetadata()
        {
            return new V1ObjectMeta
            {
                Name = "service-" + workflowNumberSource.WorkflowNumber,
                NamespaceProperty = K8sNamespace
            };
        }

        private List<V1ServicePort> CreateServicePorts(Dictionary<ContainerRecipe, Port[]> servicePorts, ContainerRecipe[] recipes)
        {
            var result = new List<V1ServicePort>();
            foreach (var recipe in recipes)
            {
                result.AddRange(CreateServicePorts(servicePorts, recipe));
            }
            return result;
        }

        private List<V1ServicePort> CreateServicePorts(Dictionary<ContainerRecipe, Port[]> servicePorts, ContainerRecipe recipe)
        {
            var result = new List<V1ServicePort>();
            var usedPorts = new List<Port>();
            foreach (var port in recipe.ExposedPorts)
            {
                var servicePort = workflowNumberSource.GetServicePort();
                usedPorts.Add(new Port(servicePort));

                result.Add(new V1ServicePort
                {
                    Name = GetNameForPort(recipe, port),
                    Protocol = "TCP",
                    Port = port.Number,
                    TargetPort = GetNameForPort(recipe, port),
                    NodePort = servicePort
                });                
            }

            servicePorts.Add(recipe, usedPorts.ToArray());
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

        private void WaitUntilDeploymentCreated(V1Deployment deploymentSpec)
        {
            WaitUntilDeploymentOnline(deploymentSpec.Metadata.Name);
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
                if (DateTime.UtcNow - start > cluster.K8sOperationTimeout())
                {
                    throw new TimeoutException("K8s operation timed out.");
                }

                cluster.WaitForK8sServiceDelay();
                state = predicate();
            }
        }

        #endregion

        private (string, string) FetchNewPod()
        {
            var pods = client.ListNamespacedPod(K8sNamespace).Items;

            var newPods = pods.Where(p => !knownPods.Contains(p.Name())).ToArray();
            if (newPods.Length != 1) throw new InvalidOperationException("Expected only 1 pod to be created. Test infra failure.");

            var newPod = newPods.Single();
            var name = newPod.Name();
            var ip = newPod.Status.PodIP;

            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("Invalid pod name received. Test infra failure.");
            if (string.IsNullOrEmpty(ip)) throw new InvalidOperationException("Invalid pod IP received. Test infra failure.");

            knownPods.Add(name);
            return (name, ip);
        }
    }
}
