namespace Utils
{
    public class Address
    {
        public Address(string host, int port)
        {
            Host = host;
            Port = port;
        }

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
    }
}
