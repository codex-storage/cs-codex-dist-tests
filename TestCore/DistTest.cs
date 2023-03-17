using k8s;
using k8s.Models;

namespace CodexDistTests.TestCore
{
    public abstract class DistTest
    {
        private const string k8sNamespace = "codex-test-namespace";

        private V1Namespace? activeNamespace;
        private V1Deployment? activeDeployment;
        private V1Service? activeService;

        public void CreateCodexNode()
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var client = new Kubernetes(config);

            var namespaceSpec = new V1Namespace
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = k8sNamespace,
                    Labels = new Dictionary<string, string> { { "name", k8sNamespace } }
                }
            };
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = "codex-demo",
                    NamespaceProperty = k8sNamespace
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string> { { "codex-node", "dist-test" } }
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> { { "codex-node", "dist-test" } }
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "codex-node",
                                Image = "thatbenbierens/nim-codex:sha-c9a62de",
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort
                                    {
                                        ContainerPort = 8080,
                                        Name = "codex-api-port"
                                    }
                                },
                                Env = new List<V1EnvVar>
                                {
                                    new V1EnvVar
                                    {
                                        Name = "LOG_LEVEL",
                                        Value = "WARN"
                                    }
                                }
                            }
                        }
                        }
                    }
                }
            };
            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = "codex-entrypoint",
                    NamespaceProperty = k8sNamespace
                },
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = new Dictionary<string, string> { { "codex-node", "dist-test" } },
                    Ports = new List<V1ServicePort>
                {
                    new V1ServicePort
                    {
                        Protocol = "TCP",
                        Port = 8080,
                        TargetPort = "codex-api-port",
                        NodePort = 30001
                    }
                }
                }
            };

            activeNamespace = client.CreateNamespace(namespaceSpec);
            activeDeployment = client.CreateNamespacedDeployment(deploymentSpec, k8sNamespace);
            activeService = client.CreateNamespacedService(serviceSpec, k8sNamespace);

            // todo: wait until online!
        }

        public CodexNode GetCodexNode()
        {
            return new CodexNode(30001); // matches service spec.
        }

        public void DestroyCodexNode()
        {
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var client = new Kubernetes(config);

            client.DeleteNamespacedService(activeService.Name(), k8sNamespace);
            client.DeleteNamespacedDeployment(activeDeployment.Name(), k8sNamespace);
            client.DeleteNamespace(activeNamespace.Name());

            // todo: wait until terminated!
        }
    }
}
