namespace Utils
{
    public static class Str
    {
        public static string Between(string input, string open, string close)
        {
            var openI = input.IndexOf(open);
            if (openI == -1) return input;
            var openIndex = openI + open.Length;
            var closeIndex = input.LastIndexOf(close);
            if (closeIndex == -1) return input;

            return input.Substring(openIndex, closeIndex - openIndex);
        }
    }
}
