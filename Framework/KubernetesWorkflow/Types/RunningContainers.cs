using Newtonsoft.Json;

namespace KubernetesWorkflow.Types
{
    public class RunningContainers
    {
        public RunningContainers(StartupConfig startupConfig, StartResult startResult, RunningContainer[] containers)
        {
            StartupConfig = startupConfig;
            StartResult = startResult;
            Containers = containers;

            foreach (var c in containers) c.RunningContainers = this;
        }

        public StartupConfig StartupConfig { get; }
        public StartResult StartResult { get; }
        public RunningContainer[] Containers { get; }

        [JsonIgnore]
        public string Name
        {
            get { return $"{Containers.Length}x '{Containers.First().Name}'"; }
        }

        public string Describe()
        {
            return string.Join(",", Containers.Select(c => c.Name));
        }
    }

    public static class RunningContainersExtensions
    {
        public static RunningContainer[] Containers(this RunningContainers[] runningContainers)
        {
            return runningContainers.SelectMany(c => c.Containers).ToArray();
        }

        public static string Describe(this RunningContainers[] runningContainers)
        {
            return string.Join(",", runningContainers.Select(c => c.Describe()));
        }
    }
}
