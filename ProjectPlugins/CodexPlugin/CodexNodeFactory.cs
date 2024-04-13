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
            var ethAddress = GetEthAddress(access);
            var marketplaceAccess = GetMarketplaceAccess(access, ethAddress);
            return new CodexNode(tools, access, group, marketplaceAccess, ethAddress);
        }

        private IMarketplaceAccess GetMarketplaceAccess(CodexAccess codexAccess, EthAddress? ethAddress)
        {
            if (ethAddress == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(tools.GetLog(), codexAccess);
        }

        private EthAddress? GetEthAddress(CodexAccess access)
        {
            var ethAccount = access.Container.Containers.Single().Recipe.Additionals.Get<EthAccount>();
            if (ethAccount == null) return null;
            return ethAccount.EthAddress;
        }

        public CrashWatcher CreateCrashWatcher(RunningContainer c)
        {
            return tools.CreateWorkflow().CreateCrashWatcher(c);
        }
    }
}
