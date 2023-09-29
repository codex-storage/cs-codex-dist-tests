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

        if (!args.Any(a => a == "-y"))
        {
            Console.WriteLine("Does the above config look good? [y/n]");
            if (Console.ReadLine()!.ToLowerInvariant() != "y") return;
            Console.WriteLine("I think so too.");
        }

        var deployment = deployer.Deploy();

        Console.WriteLine("Writing codex-deployment.json...");

        File.WriteAllText("codex-deployment.json", JsonConvert.SerializeObject(deployment, Formatting.Indented));

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
