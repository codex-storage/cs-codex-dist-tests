using ArgsUniform;
using ContinuousTests;
using DistTestCore;
using DistTestCore.Codex;
using Logging;
using Newtonsoft.Json;

public class Program
{
    public static void Main(string[] args)
    {
        var nl = Environment.NewLine;
        Console.WriteLine("CodexNetDownloader" + nl);

        var uniformArgs = new ArgsUniform<CodexNetDownloader.Configuration>(PrintHelp, args);
        var config = uniformArgs.Parse(true);

        config.CodexDeployment = ParseCodexDeploymentJson(config.CodexDeploymentJson);

        if (!Directory.Exists(config.OutputPath)) Directory.CreateDirectory(config.OutputPath);

        var k8sFactory = new K8sFactory();
        var lifecycle = k8sFactory.CreateTestLifecycle(config.KubeConfigFile, config.OutputPath, "dataPath", config.CodexDeployment.Metadata.KubeNamespace, new DefaultTimeSet(), new NullLog());

        foreach (var container in config.CodexDeployment.CodexContainers)
        {
            lifecycle.DownloadLog(container);
        }

        Console.WriteLine("Done!");
    }

    private static CodexDeployment ParseCodexDeploymentJson(string filename)
    {
        var d = JsonConvert.DeserializeObject<CodexDeployment>(File.ReadAllText(filename))!;
        if (d == null) throw new Exception("Unable to parse " + filename);
        return d;
    }

    private static void PrintHelp()
    {
        var nl = Environment.NewLine;
        Console.WriteLine("CodexNetDownloader lets you download all container logs given a codex-deployment.json file." + nl);

        Console.WriteLine("CodexNetDownloader assumes you are running this tool from *inside* the Kubernetes cluster. " +
            "If you are not running this from a container inside the cluster, add the argument '--external'." + nl);
    }
}
