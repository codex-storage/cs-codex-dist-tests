using Core;
using GethPlugin;

namespace CodexPlugin
{
    public interface ICodexNodeFactory
    {
        CodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group);
    }

    public class CodexNodeFactory : ICodexNodeFactory
    {
        private readonly IPluginTools tools;

        public CodexNodeFactory(IPluginTools tools)
        {
            this.tools = tools;
        }

        public CodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group)
        {
            var ethAddress = GetEthAddress(access);
            var marketplaceAccess = GetMarketplaceAccess(access, ethAddress);
            return new CodexNode(tools, access, group, marketplaceAccess, ethAddress);
        }

        private IMarketplaceAccess GetMarketplaceAccess(CodexAccess codexAccess, IEthAddress? ethAddress)
        {
            if (ethAddress == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(tools.GetLog(), codexAccess);
        }

        private IEthAddress? GetEthAddress(CodexAccess access)
        {
            var mStart = access.Container.Recipe.Additionals.SingleOrDefault(a => a is MarketplaceStartResults) as MarketplaceStartResults;
            if (mStart == null) return null;
            return mStart.EthAddress;
        }
    }
}
