namespace CodexNetDeployer
{
    public class ArgOrVar
    {
        public static readonly ArgVar CodexImage = new ArgVar("codex-image", "CODEXIMAGE", "Docker image of Codex.");
        public static readonly ArgVar GethImage = new ArgVar("geth-image", "GETHIMAGE", "Docker image of Geth.");
        public static readonly ArgVar ContractsImage = new ArgVar("contracts-image", "CONTRACTSIMAGE", "Docker image of Codex Contracts deployer.");
        public static readonly ArgVar KubeConfigFile = new ArgVar("kube-config", "KUBECONFIG", "Path to Kubeconfig file.");
        public static readonly ArgVar KubeNamespace = new ArgVar("kube-namespace", "KUBENAMESPACE", "Kubernetes namespace to be used for deployment.");
        public static readonly ArgVar NumberOfCodexNodes = new ArgVar("nodes", "NODES", "Number of Codex nodes to be created.");
        public static readonly ArgVar NumberOfValidatorNodes = new ArgVar("validators", "VALIDATORS", "Number of Codex nodes that will be validating.");
        public static readonly ArgVar StorageQuota = new ArgVar("storage-quota", "STORAGEQUOTA", "Storage quota in megabytes used by each Codex node.");
        public static readonly ArgVar LogLevel = new ArgVar("log-level", "LOGLEVEL", "Log level used by each Codex node. [Trace, Debug*, Info, Warn, Error]");

        private readonly string[] args;

        public ArgOrVar(string[] args)
        {
            this.args = args;
        }

        public string Get(ArgVar key, string defaultValue = "")
        {
            var argKey = $"--{key.Arg}=";
            var arg = args.FirstOrDefault(a => a.StartsWith(argKey));
            if (arg != null)
            {
                return arg.Substring(argKey.Length);
            }

            var env = Environment.GetEnvironmentVariable(key.Var);
            if (env != null)
            {
                return env;
            }

            return defaultValue;
        }
        
        public int? GetInt(ArgVar key)
        {
            var str = Get(key);
            if (string.IsNullOrEmpty(str)) return null;
            if (int.TryParse(str, out int result))
            {
                return result;
            }
            return null;
        }

        public void PrintHelp()
        {
            var nl = Environment.NewLine;
            Console.WriteLine("CodexNetDeployer allows you to easily deploy multiple Codex nodes in a Kubernetes cluster. " +
                "The deployer will set up the required supporting services, deploy the Codex on-chain contracts, start and bootstrap the Codex instances. " +
                "All Kubernetes objects will be created in the namespace provided, allowing you to easily find, modify, and delete them afterwards." + nl);

            Console.WriteLine("CodexNetDeployer assumes you are running this tool from *inside* the Kubernetes cluster you want to deploy to. " +
                "If you are not running this from a container inside the cluster, add the argument '--external'." + nl);

            Console.Write("\t[ CLI argument ] or [ Environment variable ]");
            Console.CursorLeft = 70;
            Console.Write("(Description)" + nl);
            var fields = GetType().GetFields();
            foreach (var field in fields)
            {
                var value = (ArgVar)field.GetValue(null)!;
                value.PrintHelp();
            }
        }
    }

    public class ArgVar
    {
        public ArgVar(string arg, string var, string description)
        {
            Arg = arg;
            Var = var;
            Description = description;
        }

        public string Arg { get; }
        public string Var { get; }
        public string Description { get; }

        public void PrintHelp()
        {
            Console.Write($"\t[ --{Arg}=... ] or [ {Var}=... ]");
            Console.CursorLeft = 70;
            Console.Write(Description + Environment.NewLine);
        }
    }
}
