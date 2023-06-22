using CodexNetDeployer;
using DistTestCore.Codex;
using DistTestCore.Marketplace;

public class Program
{
    public static void Main(string[] args)
    {
        var nl = Environment.NewLine;
        Console.WriteLine("CodexNetDeployer" + nl + nl);

        var argOrVar = new ArgOrVar(args);

        if (args.Any(a => a == "-h" || a == "--help" || a == "-?"))
        {
            argOrVar.PrintHelp();
            return;
        }

        var config = new Configuration(
            codexImage: argOrVar.Get(ArgOrVar.CodexImage, CodexContainerRecipe.DockerImage),
            gethImage: argOrVar.Get(ArgOrVar.GethImage, GethContainerRecipe.DockerImage),
            contractsImage: argOrVar.Get(ArgOrVar.ContractsImage, CodexContractsContainerRecipe.DockerImage),
            kubeConfigFile: argOrVar.Get(ArgOrVar.KubeConfigFile),
            kubeNamespace: argOrVar.Get(ArgOrVar.KubeNamespace),
            numberOfCodexNodes: argOrVar.GetInt(ArgOrVar.NumberOfCodexNodes),
            storageQuota: argOrVar.GetInt(ArgOrVar.StorageQuota)
        );

        Console.WriteLine("Using:");
        config.PrintConfig();
        Console.WriteLine(nl);

        var errors = config.Validate();
        if (errors.Any())
        {
            Console.WriteLine($"Configuration errors: ({errors.Count})");
            foreach ( var error in errors ) Console.WriteLine("\t" + error);
            Console.WriteLine(nl);
            argOrVar.PrintHelp();
            return;
        }


    }
}
