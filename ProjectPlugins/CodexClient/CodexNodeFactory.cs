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
        private readonly CodexHooksFactory hooksFactory;
        private readonly IHttpFactory httpFactory;
        private readonly IProcessControlFactory processControlFactory;

        public CodexNodeFactory(ILog log, IFileManager fileManager, CodexHooksFactory hooksFactory, IHttpFactory httpFactory, IProcessControlFactory processControlFactory)
        {
            this.log = log;
            this.fileManager = fileManager;
            this.hooksFactory = hooksFactory;
            this.httpFactory = httpFactory;
            this.processControlFactory = processControlFactory;
        }

        public CodexNodeFactory(ILog log, string dataDir)
            : this(log, new FileManager(log, dataDir), new CodexHooksFactory(), new HttpFactory(log), new DoNothingProcessControlFactory())
        {
        }

        public ICodexNode CreateCodexNode(ICodexInstance instance)
        {
            var processControl = processControlFactory.CreateProcessControl(instance);
            var access = new CodexAccess(log, httpFactory, processControl, instance);
            var hooks = hooksFactory.CreateHooks(access.GetName());
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
