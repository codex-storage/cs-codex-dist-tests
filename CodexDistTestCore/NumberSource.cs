namespace CodexDistTestCore
{
    public class NumberSource
    {
        private int freePort;
        private int nodeOrderNumber;

        public NumberSource()
        {
            freePort = 30001;
            nodeOrderNumber = 0;
        }

        public int GetFreePort()
        {
            var port = freePort;
            freePort++;
            return port;
        }

        public int GetNodeOrderNumber()
        {
            var number = nodeOrderNumber;
            nodeOrderNumber++;
            return number;
        }
    }
}
