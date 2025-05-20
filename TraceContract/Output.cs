using System.Numerics;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using Logging;
using Utils;

namespace TraceContract
{
    public class Output
    {
        private class Entry
        {
            public Entry(DateTime utc, string msg)
            {
                Utc = utc;
                Msg = msg;
            }

            public DateTime Utc { get; }
            public string Msg { get; }
        }

        private readonly ILog log;
        private readonly List<Entry> entries = new();
        private readonly string folder;
        private readonly List<string> files = new();

        public Output(ILog log, Input input, Config config)
        {
            folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);

            var filename = Path.Combine(folder, $"Contract_{input.PurchaseId}");
            var fileLog = new FileLog(filename);
            files.Add(fileLog.FullFilename);
            foreach (var pair in config.LogReplacements)
            {
                fileLog.AddStringReplace(pair.Key, pair.Value);
                fileLog.AddStringReplace(pair.Key.ToLowerInvariant(), pair.Value);
            }

            log.Log($"Logging to '{filename}'");
            this.log = new LogSplitter(fileLog, log);
        }

        public void LogRequestCreated(RequestEvent requestEvent)
        {
            Add(requestEvent.Block.Utc, $"Storage request created: '{requestEvent.Request.Request.Id}'");
        }

        public void LogRequestCancelled(RequestEvent requestEvent)
        {
            Add(requestEvent.Block.Utc, "Expired");
        }

        public void LogRequestFailed(RequestEvent requestEvent)
        {
            Add(requestEvent.Block.Utc, "Failed");
        }

        public void LogRequestFinished(RequestEvent requestEvent)
        {
            Add(requestEvent.Block.Utc, "Finished");
        }

        public void LogRequestStarted(RequestEvent requestEvent)
        {
            Add(requestEvent.Block.Utc, "Started");
        }

        public void LogSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            Add(requestEvent.Block.Utc, $"Slot filled. Index: {slotIndex} Host: '{host}'");
        }

        public void LogSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            Add(requestEvent.Block.Utc, $"Slot freed. Index: {slotIndex}");
        }

        public void LogSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            Add(requestEvent.Block.Utc, $"Slot reservations full. Index: {slotIndex}");
        }

        public void LogReserveSlotCalls(ReserveSlotFunction[] reserveSlotFunctions)
        {
            foreach (var call in reserveSlotFunctions) LogReserveSlotCall(call);
        }

        public void WriteContractEvents()
        {
            var sorted = entries.OrderBy(e => e.Utc).ToArray();
            foreach (var e in sorted) Write(e);
        }

        private void Write(Entry e)
        {
            log.Log($"[{Time.FormatTimestamp(e.Utc)}] {e.Msg}");
        }

        private void LogReserveSlotCall(ReserveSlotFunction call)
        {
            Add(call.Block.Utc, $"Reserve-slot called. Index: {call.SlotIndex} Host: '{call.FromAddress}'");
        }

        private void Add(DateTime utc, string msg)
        {
            entries.Add(new Entry(utc, msg));
        }

        public LogFile CreateNodeLogTargetFile(string node)
        {
            var file = log.CreateSubfile(node);
            files.Add(file.Filename);
            return file;
        }
    }
}
