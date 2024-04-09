namespace KubernetesWorkflow
{
    internal enum RunnerLocation
    {
        Unknown,
        ExternalToCluster,
        InternalToCluster,
    }

    internal static class RunnerLocationUtils
    {
        private static RunnerLocation location = RunnerLocation.Unknown;

        internal static RunnerLocation GetRunnerLocation()
        {
            DetermineRunnerLocation();
            if (location == RunnerLocation.Unknown) throw new Exception("Runner location is unknown.");
            return location;
        }

        private static void DetermineRunnerLocation()
        {
            if (location != RunnerLocation.Unknown) return;

            var port = Environment.GetEnvironmentVariable("KUBERNETES_PORT");
            var host = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");

            if (string.IsNullOrEmpty(port) || string.IsNullOrEmpty(host))
            {
                location = RunnerLocation.ExternalToCluster;
            }
            else
            {
                location = RunnerLocation.InternalToCluster;
            }
        }
    }
}
