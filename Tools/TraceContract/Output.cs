using System.Numerics;
using BlockchainUtils;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace TraceContract
{
    public class Output
    {
        private class Entry
        {
            public Entry(BlockTimeEntry blk, string msg)
            {
                Blk = blk;
                Msg = msg;
            }

            public BlockTimeEntry Blk { get; }
            public string Msg { get; }
        }

        private readonly ILog log;
        private readonly List<Entry> entries = new();
        private readonly string folder;
        private readonly Input input;
        private readonly Config config;

        public Output(ILog log, Input input, Config config)
        {
            this.input = input;
            this.config = config;

            folder = config.GetOuputFolder();
            Directory.CreateDirectory(folder);

            var filename = Path.Combine(folder, $"contract_{input.PurchaseId}");
            var fileLog = new FileLog(filename);
            log.Log($"Logging to '{filename}'");

            this.log = new LogSplitter(fileLog, log);
            foreach (var pair in config.LogReplacements)
            {
                this.log.AddStringReplace(pair.Key, pair.Value);
                this.log.AddStringReplace(pair.Key.ToLowerInvariant(), pair.Value);
            }
        }

        public void LogRequestCreated(RequestEvent requestEvent)
        {
            var r = requestEvent.Request.Request;
            var msg = $"Storage request created: '{r.Id}' = {Environment.NewLine}${JsonConvert.SerializeObject(r, Formatting.Indented)}{Environment.NewLine}";
            Add(requestEvent.Block, msg);
        }

        public void LogRequestCancelled(RequestEvent requestEvent)
        {
            Add(requestEvent.Block, "Expired");
        }

        public void LogRequestFailed(RequestEvent requestEvent)
        {
            Add(requestEvent.Block, "Failed");
        }

        public void LogRequestFinished(RequestEvent requestEvent)
        {
            Add(requestEvent.Block, "Finished");
        }

        public void LogRequestStarted(RequestEvent requestEvent)
        {
            Add(requestEvent.Block, "Started");
        }

        public void LogSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex, bool isRepair)
        {
            Add(requestEvent.Block, $"Slot filled. Index: {slotIndex} Host: '{host}' isRepair: {isRepair}");
        }

        public void LogSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            Add(requestEvent.Block, $"Slot freed. Index: {slotIndex}");
        }

        public void LogSlotReservationsFull(RequestEvent requestEvent, BigInteger slotIndex)
        {
            Add(requestEvent.Block, $"Slot reservations full. Index: {slotIndex}");
        }

        public void WriteContractEvents()
        {
            var sorted = entries.OrderBy(e => e.Blk.Utc).ToArray();
            foreach (var e in sorted) Write(e);
        }

        public LogFile CreateNodeLogTargetFile(string node)
        {
            return log.CreateSubfile(node);
        }

        public void ShowOutputFiles(ILog console)
        {
            console.Log("Files in output folder:");
            var files = Directory.GetFiles(folder);
            foreach (var file in files) console.Log(file);
        }

        private void Write(Entry e)
        {
            log.Log($"Block: {e.Blk.BlockNumber} [{Time.FormatTimestamp(e.Blk.Utc)}] {e.Msg}");
        }

        public void LogReserveSlotCall(ReserveSlotFunction call)
        {
            Add(call.Block, $"Reserve-slot called. Block: {call.Block.BlockNumber} Index: {call.SlotIndex} Host: '{call.FromAddress}'");
        }

        private void Add(BlockTimeEntry blk, string msg)
        {
            entries.Add(new Entry(blk, msg));
        }
    }
}
