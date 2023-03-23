namespace CodexDistTestCore
{
    public class NumberSource
    {
        private int number;

        public NumberSource(int start)
        {
            number = start;
        }

        public int GetNextNumber()
        {
            var n = number;
            number++;
            return n;
        }
    }
}
