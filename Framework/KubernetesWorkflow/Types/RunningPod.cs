using Newtonsoft.Json;

namespace KubernetesWorkflow.Types
{
    public class RunningPod
    {
        public RunningPod(string id, StartupConfig startupConfig, StartResult startResult, RunningContainer[] containers)
        {
            Id = id;
            StartupConfig = startupConfig;
            StartResult = startResult;
            Containers = containers;

            foreach (var c in containers) c.RunningPod = this;
        }

        public string Id { get; }
        public StartupConfig StartupConfig { get; }
        public StartResult StartResult { get; }
        public RunningContainer[] Containers { get; }

        [JsonIgnore]
        public string Name
        {
            get { return $"'{string.Join("&", Containers.Select(c => c.Name).ToArray())}'"; }
        }

        [JsonIgnore]
        public bool IsStopped { get; internal set; }

        public string Describe()
        {
            return string.Join(",", Containers.Select(c => c.Name));
        }

        public override bool Equals(object? obj)
        {
            return obj is RunningPod pod &&
                   Id == pod.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
        {
            if (IsStopped) return Name + " (*)";
            return Name;
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
