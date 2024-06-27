using Core;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public interface ICodexNodeFactory
    {
        CodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group);
        CrashWatcher CreateCrashWatcher(RunningContainer c);
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
            var ethAccount = GetEthAccount(access);
            var marketplaceAccess = GetMarketplaceAccess(access, ethAccount);
            return new CodexNode(tools, access, group, marketplaceAccess, ethAccount);
        }

        private IMarketplaceAccess GetMarketplaceAccess(CodexAccess codexAccess, EthAccount? ethAccount)
        {
            if (ethAccount == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(tools.GetLog(), codexAccess);
        }

        private EthAccount? GetEthAccount(CodexAccess access)
        {
            var ethAccount = access.Container.Containers.Single().Recipe.Additionals.Get<EthAccount>();
            if (ethAccount == null) return null;
            return ethAccount;
        }

        public CrashWatcher CreateCrashWatcher(RunningContainer c)
        {
            return tools.CreateWorkflow().CreateCrashWatcher(c);
        }
    }
}
