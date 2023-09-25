namespace WakuPlugin
{
    public interface IWakuSetup
    {
        IWakuSetup WithName(string name);
        IWakuSetup WithBootstrapNode(IWakuNode node);
    }

    public class WakuSetup : IWakuSetup
    {
        internal string? Name { get; private set; }
        internal string? BootstrapEnr { get; private set; }

        public IWakuSetup WithName(string name)
        {
            Name = name;
            return this;
        }

        public IWakuSetup WithBootstrapNode(IWakuNode node)
        {
            BootstrapEnr = node.DebugInfo().enrUri;
            return this;
        }
    }
}
