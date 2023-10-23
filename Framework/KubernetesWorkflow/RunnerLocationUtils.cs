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
            knownLocation = PingForLocation(container);
            return knownLocation.Value;
        }

        private static RunnerLocation PingForLocation(RunningContainer container)
        {
            if (PingHost(container.Pod.PodInfo.Ip))
            {
                return RunnerLocation.InternalToCluster;
            }

            foreach (var port in container.ContainerPorts)
            {
                if (PingHost(Format(port.ExternalAddress)))
                {
                    return RunnerLocation.ExternalToCluster;
                }
            }

            throw new Exception("Unable to determine location relative to kubernetes cluster.");
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
