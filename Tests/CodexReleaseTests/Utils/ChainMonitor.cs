using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using Logging;

namespace CodexReleaseTests.Utils
{
    public class ChainMonitor
    {
        private readonly ILog log;
        private readonly ICodexContracts contracts;
        private readonly DateTime startUtc;
        private readonly TimeSpan updateInterval;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task worker = Task.CompletedTask;

        public ChainMonitor(ILog log, ICodexContracts contracts, DateTime startUtc)
            : this(log, contracts, startUtc, TimeSpan.FromSeconds(3.0))
        {
        }

        public ChainMonitor(ILog log, ICodexContracts contracts, DateTime startUtc, TimeSpan updateInterval)
        {
            this.log = log;
            this.contracts = contracts;
            this.startUtc = startUtc;
            this.updateInterval = updateInterval;
        }

        public void Start(Action onFailure)
        {
            cts = new CancellationTokenSource();
            worker = Task.Run(() => Worker(onFailure));
        }

        public void Stop()
        {
            cts.Cancel();
            worker.Wait();
            if (worker.Exception != null) throw worker.Exception;
        }

        private void Worker(Action onFailure)
        {
            var state = new ChainState(log, contracts, new DoNothingChainEventHandler(), startUtc, doProofPeriodMonitoring: true);
            Thread.Sleep(updateInterval);

            log.Log("Chain monitoring started");
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    UpdateChainState(state);
                }
                catch (Exception ex)
                {
                    log.Error("Exception in chain monitor: " + ex);
                    onFailure();
                    throw;
                }

                cts.Token.WaitHandle.WaitOne(updateInterval);
            }
        }

        private void UpdateChainState(ChainState state)
        {
            state.Update();

            var reports = state.PeriodMonitor.GetAndClearReports();
            if (reports.IsEmpty) return;

            var slots = reports.Reports.Sum(r => Convert.ToInt32(r.TotalNumSlots));
            var required = reports.Reports.Sum(r => Convert.ToInt32(r.TotalProofsRequired));
            var missed = reports.Reports.Sum(r => r.MissedProofs.Length);

            log.Log($"Proof report: Slots={slots} Required={required} Missed={missed}");
        }
    }
}
