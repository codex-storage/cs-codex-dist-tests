using CodexPlugin.Hooks;
using Core;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public interface ICodexNodeFactory
    {
        CodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group);
        ContainerCrashWatcher CreateCrashWatcher(RunningContainer c);
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

        public CodexNode CreateOnlineCodexNode(CodexAccess access, CodexNodeGroup group)
        {
            var ethAccount = GetEthAccount(access);
            var hooks = codexHooksFactory.CreateHooks(access.Container.Name);

            var marketplaceAccess = GetMarketplaceAccess(access, ethAccount, hooks);
            return new CodexNode(tools, access, group, marketplaceAccess, hooks, ethAccount);
        }

        private IMarketplaceAccess GetMarketplaceAccess(CodexAccess codexAccess, EthAccount? ethAccount, ICodexNodeHooks hooks)
        {
            if (ethAccount == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(tools.GetLog(), codexAccess, hooks);
        }

        private EthAccount? GetEthAccount(CodexAccess access)
        {
            var ethAccount = access.Container.Containers.Single().Recipe.Additionals.Get<EthAccount>();
            if (ethAccount == null) return null;
            return ethAccount;
        }

        public ContainerCrashWatcher CreateCrashWatcher(RunningContainer c)
        {
            return tools.CreateWorkflow().CreateCrashWatcher(c);
        }
    }
}
