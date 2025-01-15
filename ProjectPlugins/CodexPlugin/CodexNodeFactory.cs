using CodexPlugin.Hooks;
using Core;

namespace CodexPlugin
{
    public interface ICodexNodeFactory
    {
        CodexNode CreateOnlineCodexNode(CodexAccess access);
    }

    public class CodexNodeFactory : ICodexNodeFactory
    {
        private readonly IPluginTools tools;
        private readonly CodexHooksFactory codexHooksFactory;

        public CodexNodeFactory(IPluginTools tools, CodexHooksFactory codexHooksFactory)
        {
            this.tools = tools;
            this.codexHooksFactory = codexHooksFactory;
        }

        public CodexNode CreateOnlineCodexNode(CodexAccess access)
        {
            var hooks = codexHooksFactory.CreateHooks(access.GetName());
            var marketplaceAccess = GetMarketplaceAccess(access, hooks);
            return new CodexNode(tools, access, marketplaceAccess, hooks);
        }

        private IMarketplaceAccess GetMarketplaceAccess(CodexAccess codexAccess, ICodexNodeHooks hooks)
        {
            if (codexAccess.GetEthAccount() == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(tools.GetLog(), codexAccess, hooks);
        }
    }
}
