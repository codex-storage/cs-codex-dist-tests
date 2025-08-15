using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using Logging;
using Utils;

namespace CodexReleaseTests.Utils
{
    public class ChainMonitor
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;
        private readonly ICodexContracts contracts;
        private readonly DateTime startUtc;
        private readonly TimeSpan updateInterval;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Task worker = Task.CompletedTask;

        public ChainMonitor(ILog log, IGethNode gethNode, ICodexContracts contracts, DateTime startUtc)
            : this(log, gethNode, contracts, startUtc, TimeSpan.FromSeconds(3.0))
        {
        }

        public ChainMonitor(ILog log, IGethNode gethNode, ICodexContracts contracts, DateTime startUtc, TimeSpan updateInterval)
        {
            this.log = log;
            this.gethNode = gethNode;
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
            var state = new ChainState(log, gethNode, contracts, new DoNothingThrowingChainEventHandler(), startUtc, doProofPeriodMonitoring: true);
            Thread.Sleep(updateInterval);

            log.Log($"Chain monitoring started. Update interval: {Time.FormatDuration(updateInterval)}");
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
        }
    }
}
