namespace CodexPlugin.OverwatchSupport
{
    public class NameIdMap
    {
        private readonly Dictionary<string, string> map = new Dictionary<string, string>();

        public void Add(string name, string peerId)
        {
            map.Add(name, peerId);
        }

        public string GetPeerId(string name)
        {
            return map[name];
        }
    }
}
