using Core;

namespace GethPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IGethNode StartGethNode(this CoreInterface ci, Action<IGethSetup> setup)
        {
            var p = Plugin(ci);
            return p.WrapGethContainer(p.StartGeth(setup));
        }

        private static GethPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<GethPlugin>();
        }
    }
}
