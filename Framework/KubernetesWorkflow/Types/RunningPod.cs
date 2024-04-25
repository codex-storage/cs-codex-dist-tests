using Newtonsoft.Json;

namespace KubernetesWorkflow.Types
{
    public class RunningPod
    {
        public RunningPod(StartupConfig startupConfig, StartResult startResult, RunningContainer[] containers)
        {
            StartupConfig = startupConfig;
            StartResult = startResult;
            Containers = containers;

            foreach (var c in containers) c.RunningPod = this;
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
        public static string Describe(this RunningPod[] runningContainers)
        {
            return string.Join(",", runningContainers.Select(c => c.Describe()));
        }
    }
}
