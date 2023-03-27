using CodexDistTestCore.Config;
using k8s.Models;

namespace CodexDistTestCore
{
    public class K8sPrometheusSpecs
    {
        public const string ContainerName = "dtest-prom";
        public const string ConfigFilepath = "/etc/prometheus/prometheus.yml";
        private const string dockerImage = "prom/prometheus:v2.30.3";
        private const string portName = "prom-1";

        public string GetDeploymentName()
        {
            return "test-prom";
        }

        public V1Deployment CreatePrometheusDeployment()
        {
            var deploymentSpec = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Metadata =  new V1ObjectMeta
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
                                    Image = dockerImage,
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = 9090,
                                            Name = portName
                                        }
                                    },
                                    Command = new List<string>
                                    {
                                        $"--web.enable-lifecycle --config.file={ConfigFilepath}"
                                    },
                                }
                            }
                        }
                    }
                }
            };

            return deploymentSpec;
        }

        public V1Service CreatePrometheusService(int servicePort)
        {
            var serviceSpec = new V1Service
            {
                ApiVersion = "v1",
                Metadata = new V1ObjectMeta
                {
                    Name = "codex-prom-service",
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
                            Name = "prom-service",
                            Protocol = "TCP",
                            Port = 9090,
                            TargetPort = portName,
                            NodePort = servicePort
                        }
                    }
                }
            };

            return serviceSpec;
        }

        private Dictionary<string, string> CreateSelector()
        {
            return new Dictionary<string, string> { { "test-prom", "dtest-prom" } };
        }
    }
}
