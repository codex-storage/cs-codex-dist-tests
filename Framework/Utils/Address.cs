﻿namespace Utils
{
    public class Address
    {
        public Address(string logName, string host, int port)
        {
            LogName = logName;
            Host = host;
            Port = port;
        }

        public string LogName { get; }
        public string Host { get; }
        public int Port { get; }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Host) && Port > 0;
        }

        public static Address Empty()
        {
            return new Address(string.Empty, string.Empty, 0);
        }
    }

    public interface IHasMetricsScrapeTarget
    {
        Address GetMetricsScrapeTarget();
    }
    
    public interface IHasManyMetricScrapeTargets
    {
        Address[] GetMetricsScrapeTargets();
    }

}
