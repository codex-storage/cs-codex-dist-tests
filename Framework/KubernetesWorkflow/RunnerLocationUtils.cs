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
        private static RunnerLocation knownLocation = RunnerLocation.Unknown;

        internal static RunnerLocation DetermineRunnerLocation(ILog log, PodInfo info, K8sCluster cluster)
        {
            if (knownLocation != RunnerLocation.Unknown) return knownLocation;
            knownLocation = PingForLocation(info, cluster);
            log.Log("Runner location set to: " + knownLocation);
            return knownLocation;
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
