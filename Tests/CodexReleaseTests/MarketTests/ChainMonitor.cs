using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using Logging;

namespace CodexReleaseTests.MarketTests
{
    public class ChainMonitor
    {
        private readonly ChainState chainMonitor;
        private readonly TimeSpan interval;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task worker = null!;

        public ChainMonitor(ILog log, ICodexContracts contracts, DateTime startUtc, TimeSpan interval)
        {
            chainMonitor = new ChainState(log, contracts, new DoNothingChainEventHandler(), startUtc, true);
            this.interval = interval;
        }

        public void Start()
        {
            cts = new CancellationTokenSource();
            worker = Task.Run(Worker);
        }

        public void Stop()
        {
            cts.Cancel();
            worker.Wait();

            worker = null!;
            cts = null!;
        }

        public PeriodMonitorResult GetPeriodReports()
        {
            return chainMonitor.PeriodMonitor.GetAndClearReports();
        }

        private void Worker()
        {
            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(interval);
                chainMonitor.Update();
            }
        }
    }
}
