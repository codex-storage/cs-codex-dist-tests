using Newtonsoft.Json;

namespace BiblioTech.Rewards
{
    public class CustomReplacement
    {
        private readonly Dictionary<string, string> replacements = new Dictionary<string, string>();
        private readonly string file;

        public CustomReplacement(Configuration config)
        {
            file = Path.Combine(config.DataPath, "logreplacements.json");
        }

        public void Load()
        {
            replacements.Clear();
            if (!File.Exists(file)) return;

            var replaces = JsonConvert.DeserializeObject<ReplaceJson[]>(File.ReadAllText(file));
            if (replaces == null) return;

            foreach (var replace in replaces)
            {
                replacements.Add(replace.From, replace.To);
            }
        }

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
            Save();
        }

        public void Remove(string from)
        {
            replacements.Remove(from);
            Save();
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

        private void Save()
        {
            ReplaceJson[] replaces = replacements.Select(pair =>
            {
                return new ReplaceJson
                {
                    From = pair.Key,
                    To = pair.Value
                };
            }).ToArray();

            File.WriteAllText(file, JsonConvert.SerializeObject(replaces));
        }

        private class ReplaceJson
        {
            public string From { get; set; } = string.Empty;
            public string To { get; set; } = string.Empty;
        }
    }
}
