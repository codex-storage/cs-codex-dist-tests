using Core;

namespace BittorrentPlugin
{
    public static class CoreInterfaceExtensions
    {
        public static IBittorrentNode StartBittorrentNode(this CoreInterface ci)
        {
            return Plugin(ci).StartNode();
        }

        private static BittorrentPlugin Plugin(CoreInterface ci)
        {
            return ci.GetPlugin<BittorrentPlugin>();
        }
    }
}
