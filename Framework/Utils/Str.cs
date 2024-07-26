namespace Utils
{
    public static class Str
    {
        public static string Between(string input, string open, string close)
        {
            var openIndex = input.IndexOf(open) + open.Length;
            var closeIndex = input.LastIndexOf(close);

            return input.Substring(openIndex, closeIndex - openIndex);
        }
    }
}
