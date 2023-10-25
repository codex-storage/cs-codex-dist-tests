using ArgsUniform;
using CodexNetDeployer;
using Newtonsoft.Json;
using Configuration = CodexNetDeployer.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var nl = Environment.NewLine;
        Console.WriteLine("CodexNetDeployer" + nl);

        var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
        var config = uniformArgs.Parse(true);
        
        var errors = config.Validate();
        if (errors.Any())
        {
            Console.WriteLine($"Configuration errors: ({errors.Count})");
            foreach ( var error in errors ) Console.WriteLine("\t" + error);
            Console.WriteLine(nl);
            PrintHelp();
            return;
        }

        var deployer = new Deployer(config);
        deployer.AnnouncePlugins();

        if (config.IsPublicTestNet || !args.Any(a => a == "-y"))
        {
            if (config.IsPublicTestNet) Console.WriteLine("Deployment is configured as public testnet.");
            Console.WriteLine("Does the above config look good? [y/n]");
            if (Console.ReadLine()!.ToLowerInvariant() != "y") return;
            if (config.IsPublicTestNet) Console.WriteLine("You better be right about that, cause it's going live right now.");
            else Console.WriteLine("I think so too.");
        }

        var deployment = deployer.Deploy();

        Console.WriteLine($"Writing deployment file '{config.DeployFile}'...");
        File.WriteAllText(config.DeployFile, JsonConvert.SerializeObject(deployment, Formatting.Indented));
        Console.WriteLine("Done!");
    }

    private static void PrintHelp()
    {
        var nl = Environment.NewLine;
        Console.WriteLine("CodexNetDeployer allows you to deploy multiple Codex nodes in a Kubernetes cluster. " +
            "The deployer will set up the required supporting services, deploy the Codex on-chain contracts, start and bootstrap the Codex instances. " +
            "All Kubernetes objects will be created in the namespace provided, allowing you to easily find, modify, and delete them afterwards." + nl);
    }
}
