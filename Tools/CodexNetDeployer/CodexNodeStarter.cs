using CodexContractsPlugin;
using CodexPlugin;
using Core;
using GethPlugin;
using Utils;

namespace CodexNetDeployer
{
    public class CodexNodeStarter
    {
        private readonly Configuration config;
        private readonly CoreInterface ci;
        private readonly IGethNode gethNode;
        private readonly ICodexContracts contracts;
        private ICodexNode? bootstrapNode = null;
        private int validatorsLeft;

        public CodexNodeStarter(Configuration config, CoreInterface ci, IGethNode gethNode, ICodexContracts contracts, int numberOfValidators)
        {
            this.config = config;
            this.ci = ci;
            this.gethNode = gethNode;
            this.contracts = contracts;
            validatorsLeft = numberOfValidators;
        }

        public CodexNodeStartResult? Start(int i)
        {
            var name = GetCodexContainerName(i);
            Console.Write($" - {i} ({name})");
            Console.CursorLeft = 30;

            ICodexNode? codexNode = null;
            try
            {
                codexNode = ci.StartCodexNode(s =>
                {
                    s.WithName(name);
                    s.WithLogLevel(config.CodexLogLevel, new CodexLogCustomTopics(config.Discv5LogLevel, config.Libp2pLogLevel));
                    s.WithStorageQuota(config.StorageQuota!.Value.MB());
                    s.EnableMarketplace(gethNode, contracts, 100.Eth(), config.InitialTestTokens.TestTokens(), validatorsLeft > 0);
                    s.EnableMetrics();

                    if (bootstrapNode != null) s.WithBootstrapNode(bootstrapNode);
                    if (config.BlockTTL != Configuration.SecondsIn1Day) s.WithBlockTTL(TimeSpan.FromSeconds(config.BlockTTL));
                    if (config.BlockMI != Configuration.TenMinutes) s.WithBlockMaintenanceInterval(TimeSpan.FromSeconds(config.BlockMI));
                    if (config.BlockMN != 1000) s.WithBlockMaintenanceNumber(config.BlockMN);
                });
            
                var debugInfo = codexNode.GetDebugInfo();
                if (!string.IsNullOrWhiteSpace(debugInfo.spr))
                {
                    Console.Write("Online\t");

                    var response = codexNode.Marketplace.MakeStorageAvailable(
                        size: config.StorageSell!.Value.MB(),
                        minPriceForTotalSpace: config.MinPrice.TestTokens(),
                        maxCollateral: config.MaxCollateral.TestTokens(),
                        maxDuration: TimeSpan.FromSeconds(config.MaxDuration));

                    if (!string.IsNullOrEmpty(response))
                    {
                        Console.Write("Storage available\tOK" + Environment.NewLine);

                        validatorsLeft--;
                        if (bootstrapNode == null) bootstrapNode = codexNode;
                        return new CodexNodeStartResult(codexNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.ToString());
            }

            Console.WriteLine("Unknown failure.");
            if (codexNode != null)
            {
                Console.WriteLine("Downloading container log.");
                ci.DownloadLog(codexNode);
            }

            return null;
        }

        private string GetCodexContainerName(int i)
        {
            if (i == 0) return "BOOTSTRAP";
            return "CODEX" + i;
        }
    }

    public class CodexNodeStartResult
    {
        public CodexNodeStartResult(ICodexNode codexNode)
        {
            CodexNode = codexNode;
        }

        public ICodexNode CodexNode { get; }
    }
}
