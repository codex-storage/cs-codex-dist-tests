using Core;

namespace GethPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IGethNode StartGethNode(this CoreInterface ci, Action<IGethSetup> setup)
        {
            return Plugin(ci).StartGeth(setup);
        }

        private static GethPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<GethPlugin>();
        }
    }
}
