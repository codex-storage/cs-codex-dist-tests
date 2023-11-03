using ArgsUniform;

namespace TestClusterStarter
{
    public class Configuration
    {
        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use 'null' (default) to use local cluster.")]
        public string KubeConfigFile { get; set; } = "null";
    }
}
