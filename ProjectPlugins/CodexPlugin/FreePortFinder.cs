using System.Net.NetworkInformation;

namespace CodexPlugin
{
    public class FreePortFinder
    {
        private readonly object _lock = new object();
        private int nextPort = 8080;

        public int GetNextFreePort()
        {
            lock (_lock)
            {
                return Next();
            }
        }

        private int Next()
        {
            while (true)
            {
                var p = nextPort;
                nextPort++;

                if (!IsInUse(p))
                {
                    return p;
                }

                if (nextPort > 30000) throw new Exception("Running out of ports.");
            }
        }

        private bool IsInUse(int port)
        {
            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
            if (ipProps.GetActiveTcpConnections().Any(t => t.LocalEndPoint.Port == port)) return true;
            if (ipProps.GetActiveTcpListeners().Any(t => t.Port == port)) return true;
            if (ipProps.GetActiveUdpListeners().Any(u => u.Port == port)) return true;
            return false;
        }
    }
}
