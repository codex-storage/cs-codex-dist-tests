using CodexNetDeployer;
using DistTestCore;
using DistTestCore.Codex;
using DistTestCore.Marketplace;
using Newtonsoft.Json;
using Utils;
using Configuration = CodexNetDeployer.Configuration;

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

        var location = TestRunnerLocation.InternalToCluster;
        if (args.Any(a => a == "--external"))
        {
            location = TestRunnerLocation.ExternalToCluster;
        }

        var config = new Configuration(
            codexImage: argOrVar.Get(ArgOrVar.CodexImage, CodexContainerRecipe.DockerImage),
            gethImage: argOrVar.Get(ArgOrVar.GethImage, GethContainerRecipe.DockerImage),
            contractsImage: argOrVar.Get(ArgOrVar.ContractsImage, CodexContractsContainerRecipe.DockerImage),
            kubeConfigFile: argOrVar.Get(ArgOrVar.KubeConfigFile),
            kubeNamespace: argOrVar.Get(ArgOrVar.KubeNamespace),
            numberOfCodexNodes: argOrVar.GetInt(ArgOrVar.NumberOfCodexNodes),
            numberOfValidators: argOrVar.GetInt(ArgOrVar.NumberOfValidatorNodes),
            storageQuota: argOrVar.GetInt(ArgOrVar.StorageQuota),
            codexLogLevel: ParseEnum.Parse<CodexLogLevel>(argOrVar.Get(ArgOrVar.LogLevel, nameof(CodexLogLevel.Debug))),
            runnerLocation: location
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

        var deployer = new Deployer(config);
        var deployment = deployer.Deploy();

        Console.WriteLine("Writing codex-deployment.json...");

        File.WriteAllText("codex-deployment.json", JsonConvert.SerializeObject(deployment, Formatting.Indented));

        Console.WriteLine("Done!");
    }
}
