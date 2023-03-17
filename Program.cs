using k8s;
using k8s.Models;

public static class Program
{
    private const string ns = "codex-test-namespace";

    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
        var client = new Kubernetes(config);

        var deploymentSpec = new V1Deployment
        {
            ApiVersion= "apps/v1",
            Metadata = new V1ObjectMeta
            {
                Name = "codex-demo",
                NamespaceProperty = ns
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string,string> { { "codex-node", "dist-test" } }
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Labels = new Dictionary<string,string> { { "codex-node", "dist-test" } }
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
                NamespaceProperty = ns
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

        Console.WriteLine("specs made");

        var ans = client.CreateNamespace(new V1Namespace
        {
            ApiVersion = "v1",
            Metadata = new V1ObjectMeta
            {
                Name = ns,
                Labels = new Dictionary<string, string> { { "name", ns } }
            }
        });

        Console.WriteLine("created namespace");

        var deployment = client.CreateNamespacedDeployment(deploymentSpec, ns);

        Console.WriteLine("deploy made");

        var service = client.CreateNamespacedService(serviceSpec, ns);

        Console.WriteLine("Service up. Press Q to close...");
        var s = "";
        while (!s.StartsWith("q"))
        {
            s = Console.ReadLine();
        }

        client.DeleteNamespacedService(service.Name(), ns);
        client.DeleteNamespacedDeployment(deployment.Name(), ns);
        client.DeleteNamespace(ans.Name());

        Console.WriteLine("Done.");
    }
}
