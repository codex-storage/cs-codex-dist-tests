using Logging;
using System.Net.NetworkInformation;

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

        private static void DetermineRunnerLocation()//ILog log, PodInfo info, K8sCluster cluster)
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

        private static RunnerLocation PingForLocation(PodInfo podInfo, K8sCluster cluster)
        {
            if (PingHost(podInfo.Ip))
            {
                return RunnerLocation.InternalToCluster;
            }

            if (PingHost(Format(cluster.HostAddress)))
            {
                return RunnerLocation.ExternalToCluster;
            }

            throw new Exception("Unable to determine location relative to kubernetes cluster.");
        }

        private static string Format(string host)
        {
            return host
                .Replace("http://", "")
                .Replace("https://", "");
        }

        private static bool PingHost(string host)
        {
            try
            {
                using var pinger = new Ping();
                PingReply reply = pinger.Send(host);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
            }

            return false;
        }
    }
}
