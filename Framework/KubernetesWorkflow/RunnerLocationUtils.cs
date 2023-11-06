using System.Net.NetworkInformation;
using Utils;

namespace KubernetesWorkflow
{
    internal enum RunnerLocation
    {
        ExternalToCluster,
        InternalToCluster,
    }

    internal static class RunnerLocationUtils
    {
        private static RunnerLocation? knownLocation = null;

        internal static RunnerLocation DetermineRunnerLocation(PodInfo info, K8sCluster cluster)
        {
            if (knownLocation != null) return knownLocation.Value;
            knownLocation = PingForLocation(info, cluster);
            return knownLocation.Value;
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
