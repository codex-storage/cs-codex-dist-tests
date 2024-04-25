namespace Utils
{
    public class NumberSource
    {
        private readonly object @lock = new object();
        private int number;

        public NumberSource(int start)
        {
            number = start;
        }

        public int GetNextNumber()
        {
            var n = -1;
            lock (@lock)
            {
                n = number;
                number++;
            }
            return n;
        }
    }
}
