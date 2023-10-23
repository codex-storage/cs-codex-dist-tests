namespace Utils
{
    public class Address
    {
        public static readonly Address InvalidAddress = new Address(string.Empty, 0);

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
    }
}
