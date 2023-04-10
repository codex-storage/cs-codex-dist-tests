using CodexDistTestCore.Config;
using k8s.Models;

namespace CodexDistTestCore.Marketplace
{
    public static class GethDockerImage
    {
        public const string Image = "thatbenbierens/geth-confenv:latest";
    }

    public class K8sGethBoostrapSpecs
    {
        public const string ContainerName = "dtest-gethb";
        private const string portName = "gethb";
        private const string genesisJsonBase64 = "ewogICAgImNvbmZpZyI6IHsKICAgICAgImNoYWluSWQiOiAxMjM0NSwKICAgICAgImhvbWVzdGVhZEJsb2NrIjogMCwKICAgICAgImVpcDE1MEJsb2NrIjogMCwKICAgICAgImVpcDE1NUJsb2NrIjogMCwKICAgICAgImVpcDE1OEJsb2NrIjogMCwKICAgICAgImJ5emFudGl1bUJsb2NrIjogMCwKICAgICAgImNvbnN0YW50aW5vcGxlQmxvY2siOiAwLAogICAgICAicGV0ZXJzYnVyZ0Jsb2NrIjogMCwKICAgICAgImlzdGFuYnVsQmxvY2siOiAwLAogICAgICAibXVpckdsYWNpZXJCbG9jayI6IDAsCiAgICAgICJiZXJsaW5CbG9jayI6IDAsCiAgICAgICJsb25kb25CbG9jayI6IDAsCiAgICAgICJhcnJvd0dsYWNpZXJCbG9jayI6IDAsCiAgICAgICJncmF5R2xhY2llckJsb2NrIjogMCwKICAgICAgImNsaXF1ZSI6IHsKICAgICAgICAicGVyaW9kIjogNSwKICAgICAgICAiZXBvY2giOiAzMDAwMAogICAgICB9CiAgICB9LAogICAgImRpZmZpY3VsdHkiOiAiMSIsCiAgICAiZ2FzTGltaXQiOiAiODAwMDAwMDAwIiwKICAgICJleHRyYWRhdGEiOiAiMHgwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwN2RmOWE4NzVhMTc0YjNiYzU2NWU2NDI0YTAwNTBlYmMxYjJkMWQ4MjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAiLAogICAgImFsbG9jIjogewogICAgICAiQUNDT1VOVF9IRVJFIjogeyAiYmFsYW5jZSI6ICI1MDAwMDAiIH0KICAgIH0KICB9";

        public K8sGethBoostrapSpecs(int servicePort)
        {
            ServicePort = servicePort;
        }

        public int ServicePort { get; }

        public string GetDeploymentName()
        {
            return "test-gethb";
        }

        public V1Deployment CreateGethBootstrapDeployment()
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata = new V1ObjectMeta
                {
                    Name = GetDeploymentName(),
                    NamespaceProperty = K8sCluster.K8sNamespace
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = CreateSelector()
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = CreateSelector()
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
                                            ContainerPort = 9090,
                                            Name = portName
                                        }
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        //new V1EnvVar
                                        //{
                                        //    Name = "GETH_ARGS",
                                        //    Value = "--qwerty"
                                        //},
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
                    Selector = CreateSelector(),
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort
                        {
                            Name = "gethb-service",
                            Protocol = "TCP",
                            Port = 9090,
                            TargetPort = portName,
                            NodePort = ServicePort
                        }
                    }
                }
            };

            return serviceSpec;
        }

        private Dictionary<string, string> CreateSelector()
        {
            return new Dictionary<string, string> { { "test-gethb", "dtest-gethb" } };
        }
    }
}
