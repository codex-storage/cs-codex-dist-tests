using ArgsUniform;
using Core;
using DeployAndRunPlugin;
using KubernetesWorkflow;
using Logging;
using Newtonsoft.Json;
using TestClusterStarter;

public class Program
{
    private const string SpecsFile = "TestSpecs.json";

    public static void Main(string[] args)
    {
        var argsUniform = new ArgsUniform<TestClusterStarter.Configuration>(() => { }, args);
        var config = argsUniform.Parse();

        ProjectPlugin.Load<DeployAndRunPlugin.DeployAndRunPlugin>();

        if (!File.Exists(SpecsFile))
        {
            File.WriteAllText(SpecsFile, JsonConvert.SerializeObject(new ClusterTestSetup(new[]
            {
                new ClusterTestSpec("example", "peer", 2, Convert.ToInt32(TimeSpan.FromDays(2).TotalSeconds), "imageoverride")
            })));
            return;
        }
        var specs = JsonConvert.DeserializeObject<ClusterTestSetup>(File.ReadAllText(SpecsFile))!;

        var kConfig = new KubernetesWorkflow.Configuration(config.KubeConfigFile, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), kubernetesNamespace: "default");
        var entryPoint = new EntryPoint(new ConsoleLog(), kConfig, "datafolder");
        var ci = entryPoint.CreateInterface();

        var rcs = new List<RunningContainer>();
        foreach (var spec in specs.Specs)
        {
            var rc = ci.DeployAndRunContinuousTests(new RunConfig(
                name: spec.Name,
                filter: spec.Filter,
                duration: TimeSpan.FromSeconds(spec.DurationSeconds),
                replications: spec.Replication,
                codexImageOverride: spec.CodexImageOverride));

            rcs.Add(rc);
        }

        var deployment = new ClusterTestDeployment(rcs.ToArray());
        File.WriteAllText("clustertest-deployment.json", JsonConvert.SerializeObject(deployment, Formatting.Indented));
    }
}
