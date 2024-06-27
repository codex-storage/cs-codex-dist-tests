namespace BiblioTech.Rewards
{
    public class CustomReplacement
    {
        private readonly Dictionary<string, string> replacements = new Dictionary<string, string>();

        public void Add(string from, string to)
        {
            if (replacements.ContainsKey(from))
            {
                replacements[from] = to;
            }
            else
            {
                replacements.Add(from, to);
            }
        }

        public void Remove(string from)
        {
            replacements.Remove(from);
        }

        public string Apply(string msg)
        {
            var result = msg;
            foreach (var pair in  replacements)
            {
                result.Replace(pair.Key, pair.Value);
            }
            return result;
        }
    }
}
