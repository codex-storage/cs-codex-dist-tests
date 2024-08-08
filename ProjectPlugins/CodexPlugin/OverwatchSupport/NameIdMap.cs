namespace CodexPlugin.OverwatchSupport
{
    public class NameIdMap
    {
        private readonly Dictionary<string, string> map = new Dictionary<string, string>();
        private readonly Dictionary<string, string> shortToLong = new Dictionary<string, string>();

        public void Add(string name, string peerId)
        {
            map.Add(name, peerId);

            shortToLong.Add(CodexUtils.ToShortId(peerId), peerId);
        }

        public string GetPeerId(string name)
        {
            return map[name];
        }

        public string ReplaceShortIds(string value)
        {
            var result = value;
            foreach (var pair in shortToLong)
            {
                result = result.Replace(pair.Key, pair.Value);
            }
            return result;
        }

        public int Size
        {
            get { return map.Count; }
        }
    }
}
