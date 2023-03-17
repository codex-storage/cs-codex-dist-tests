using k8s;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();

        var client = new Kubernetes(config);

        var namespaces = client.CoreV1.ListNamespace();
        foreach (var ns in namespaces.Items)
        {
            Console.WriteLine(ns.Metadata.Name);
            var list = client.CoreV1.ListNamespacedPod(ns.Metadata.Name);
            foreach (var item in list.Items)
            {
                Console.WriteLine(item.Metadata.Name);
            }
        }

        var services = client.CoreV1.ListServiceForAllNamespaces();
        foreach (var service in services)
        {
            Console.WriteLine($"service: {service.Metadata.Name}");
        }

        Console.WriteLine("Done.");
    }
}
