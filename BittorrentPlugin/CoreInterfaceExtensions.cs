using Core;

namespace BittorrentPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static void RunThing(this CoreInterface ci)
        {
            Plugin(ci).Run();
        }

        private static BittorrentPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<BittorrentPlugin>();
        }
    }
}
