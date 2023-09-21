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

        internal static RunnerLocation DetermineRunnerLocation(RunningContainer container)
        {
            if (knownLocation != null) return knownLocation.Value;

            if (PingHost(container.Pod.PodInfo.Ip))
            {
                knownLocation = RunnerLocation.InternalToCluster;
            }
            if (PingHost(Format(container.ClusterExternalAddress)))
            {
                knownLocation = RunnerLocation.ExternalToCluster;
            }

            if (knownLocation == null) throw new Exception("Unable to determine location relative to kubernetes cluster.");
            return knownLocation.Value;
        }

        private static string Format(Address host)
        {
            return host.Host
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
