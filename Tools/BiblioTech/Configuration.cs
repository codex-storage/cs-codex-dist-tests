using ArgsUniform;

namespace BiblioTech
{
    public class Configuration
    {
        [Uniform("token", "t", "TOKEN", true, "Discord Application Token")]
        public string ApplicationToken { get; set; } = string.Empty;

        [Uniform("server-name", "sn", "SERVERNAME", true, "Name of the Discord server")]
        public string ServerName { get; set; } = string.Empty;

        [Uniform("datapath", "dp", "DATAPATH", false, "Root path where all data files will be saved.")]
        public string DataPath { get; set; } = "datapath";
        
        [Uniform("admin-role", "a", "ADMINROLE", true, "Name of the Discord server admin role")]
        public string AdminRoleName { get; set; } = string.Empty;

        [Uniform("admin-channel-name", "ac", "ADMINCHANNELNAME", true, "Name of the Discord server channel where admin commands are allowed.")]
        public string AdminChannelName { get; set; } = "admin-channel";

        [Uniform("kube-config", "kc", "KUBECONFIG", true, "Path to Kubeconfig file. Use a Kubeconfig with read-only access.")]
        public string KubeConfigFile { get; set; } = "null";

        [Uniform("kube-namespace", "kn", "KUBENAMESPACE", true, "Kubernetes namespace.")]
        public string KubeNamespace { get; set; } = string.Empty;

        public string EndpointsPath
        {
            get
            {
                return Path.Combine(DataPath, "endpoints");
            }
        }

        public string UserDataPath
        {
            get
            {
                return Path.Combine(DataPath, "users");
            }
        }
    }
}
