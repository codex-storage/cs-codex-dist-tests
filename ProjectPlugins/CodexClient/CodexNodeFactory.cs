using CodexClient.Hooks;
using FileUtils;
using Logging;
using WebUtils;

namespace CodexClient
{
    public class CodexNodeFactory
    {
        private readonly ILog log;
        private readonly IFileManager fileManager;
        private readonly CodexHooksFactory hooksFactor;
        private readonly IHttpFactory httpFactory;
        private readonly IIProcessControlFactory processControlFactory;

        public CodexNodeFactory(ILog log, IFileManager fileManager, CodexHooksFactory hooksFactory, IHttpFactory httpFactory, IIProcessControlFactory processControlFactory)
        {
            this.log = log;
            this.fileManager = fileManager;
            this.hooksFactor = hooksFactory;
            this.httpFactory = httpFactory;
            this.processControlFactory = processControlFactory;
        }

        public ICodexNode CreateCodexNode(ICodexInstance instance)
        {
            var processControl = processControlFactory.CreateProcessControl(instance);
            var access = new CodexAccess(log, httpFactory, processControl, instance);
            var hooks = hooksFactor.CreateHooks(access.GetName());
            var marketplaceAccess = CreateMarketplaceAccess(instance, access, hooks);
            var node =  new CodexNode(log, access, fileManager, marketplaceAccess, hooks);
            node.Initialize();
            return node;
        }

        private IMarketplaceAccess CreateMarketplaceAccess(ICodexInstance instance, CodexAccess access, ICodexNodeHooks hooks)
        {
            if (instance.EthAccount == null) return new MarketplaceUnavailable();
            return new MarketplaceAccess(log, access, hooks);
        }
    }
}
