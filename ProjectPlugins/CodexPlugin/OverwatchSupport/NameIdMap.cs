namespace CodexPlugin.OverwatchSupport
{
    public class NameIdMap
    {
        private readonly Dictionary<string, CodexNodeIdentity> map = new Dictionary<string, CodexNodeIdentity>();
        private readonly Dictionary<string, string> shortToLong = new Dictionary<string, string>();

        public void Add(string name, CodexNodeIdentity identity)
        {
            map.Add(name, identity);

            shortToLong.Add(CodexUtils.ToShortId(identity.PeerId), identity.PeerId);
            shortToLong.Add(CodexUtils.ToShortId(identity.NodeId), identity.NodeId);
        }

        public CodexNodeIdentity GetId(string name)
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
