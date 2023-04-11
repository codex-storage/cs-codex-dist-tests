using CodexDistTestCore.Config;
using k8s.Models;

namespace CodexDistTestCore.Marketplace
{
    public static class GethDockerImage
    {
        public const string Image = "thatbenbierens/geth-confenv:latest";
        public const string AccountFilename = "account_string.txt";
        public const string GenesisFilename = "genesis.json";
    }

    public class K8sGethBoostrapSpecs
    {
        public const string ContainerName = "dtest-gethb";
        private const string portName = "gethb";
        private const string genesisJsonBase64 = "ewogICAgImNvbmZpZyI6IHsKICAgICAgImNoYWluSWQiOiA3ODk5ODgsCiAgICAgICJob21lc3RlYWRCbG9jayI6IDAsCiAgICAgICJlaXAxNTBCbG9jayI6IDAsCiAgICAgICJlaXAxNTVCbG9jayI6IDAsCiAgICAgICJlaXAxNThCbG9jayI6IDAsCiAgICAgICJieXphbnRpdW1CbG9jayI6IDAsCiAgICAgICJjb25zdGFudGlub3BsZUJsb2NrIjogMCwKICAgICAgInBldGVyc2J1cmdCbG9jayI6IDAsCiAgICAgICJpc3RhbmJ1bEJsb2NrIjogMCwKICAgICAgIm11aXJHbGFjaWVyQmxvY2siOiAwLAogICAgICAiYmVybGluQmxvY2siOiAwLAogICAgICAibG9uZG9uQmxvY2siOiAwLAogICAgICAiYXJyb3dHbGFjaWVyQmxvY2siOiAwLAogICAgICAiZ3JheUdsYWNpZXJCbG9jayI6IDAsCiAgICAgICJjbGlxdWUiOiB7CiAgICAgICAgInBlcmlvZCI6IDUsCiAgICAgICAgImVwb2NoIjogMzAwMDAKICAgICAgfQogICAgfSwKICAgICJkaWZmaWN1bHR5IjogIjEiLAogICAgImdhc0xpbWl0IjogIjgwMDAwMDAwMCIsCiAgICAiZXh0cmFkYXRhIjogIjB4MDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMEFDQ09VTlRfSEVSRTAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAiLAogICAgImFsbG9jIjogewogICAgICAiMHhBQ0NPVU5UX0hFUkUiOiB7ICJiYWxhbmNlIjogIjUwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAiIH0KICAgIH0KICB9";

        public K8sGethBoostrapSpecs(int servicePort)
        {
            ServicePort = servicePort;
        }

        public int ServicePort { get; }

        public string GetBootstrapDeploymentName()
        {
            return "test-gethb";
        }

        public string GetCompanionDeploymentName(GethCompanionGroup group)
        {
            return "test-geth" + group.Number;
        }

        public V1Deployment CreateGethBootstrapDeployment()
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = GetBootstrapDeploymentName(),
                    NamespaceProperty = K8sCluster.K8sNamespace
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = CreateBootstrapSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = CreateBootstrapSelector()
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = ContainerName,
                                    Image = GethDockerImage.Image,
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = 8545,
                                            Name = portName
                                        }
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "GETH_ARGS",
                                            Value = ""
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "GENESIS_JSON",
                                            Value = genesisJsonBase64
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "IS_BOOTSTRAP",
                                            Value = "1"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return deploymentSpec;
        }

        public V1Service CreateGethBootstrapService()
        {
            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = "codex-gethb-service",
                    NamespaceProperty = K8sCluster.K8sNamespace
                },
                Spec = new V1ServiceSpec
                {
                    Type = "NodePort",
                    Selector = CreateBootstrapSelector(),
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort
                        {
                            Name = "gethb-service",
                            Protocol = "TCP",
                            Port = 8545,
                            TargetPort = portName,
                            NodePort = ServicePort
                        }
                    }
                }
            };

            return serviceSpec;
        }

        public V1Deployment CreateGethCompanionDeployment(GethCompanionGroup group, GethBootstrapInfo info)
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = GetCompanionDeploymentName(group),
                    NamespaceProperty = K8sCluster.K8sNamespace
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = CreateCompanionSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = CreateCompanionSelector()
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = group.Containers.Select(c => CreateContainer(c, info)).ToList()
                        }
                    }
                }
            };

            return deploymentSpec;
        }

        private static V1Container CreateContainer(GethCompanionNodeContainer container, GethBootstrapInfo info)
        {
            return new V1Container
            {
                Name = container.Name,
                Image = GethDockerImage.Image,
                Ports = new List<V1ContainerPort>
                    {
                        new V1ContainerPort
                        {
                            ContainerPort = container.ApiPort,
                            Name = container.ContainerPortName
                        }
                    },
                // todo: use env vars to connect this node to the bootstrap node provided by gethInfo.podInfo & gethInfo.servicePort & gethInfo.genesisJsonBase64
                Env = new List<V1EnvVar>
                {
                    new V1EnvVar
                    {
                        Name = "GETH_ARGS",
                        Value = $"--port {container.ApiPort} --discovery.port {container.ApiPort} --http.port {container.RpcPort}"
                    },
                    new V1EnvVar
                    {
                        Name = "GENESIS_JSON",
                        Value = info.GenesisJsonBase64
                    }
                }
            };
        }

        private Dictionary<string, string> CreateBootstrapSelector()
        {
            return new Dictionary<string, string> { { "test-gethb", "dtest-gethb" } };
        }

        private Dictionary<string, string> CreateCompanionSelector()
        {
            return new Dictionary<string, string> { { "test-gethc", "dtest-gethc" } };
        }
    }
}
