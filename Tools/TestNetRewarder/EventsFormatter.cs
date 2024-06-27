using CodexContractsPlugin;
using CodexContractsPlugin.ChainMonitor;
using GethPlugin;
using System.Numerics;
using Utils;

namespace TestNetRewarder
{
    public class EventsFormatter : IChainStateChangeHandler
    {
        private static readonly string nl = Environment.NewLine;
        private readonly List<string> events = new List<string>();

        public string[] GetEvents()
        {
            var result = events.ToArray();
            events.Clear();
            return result;
        }

        public void AddError(string error)
        {
            AddBlock("📢 **Error**", error);
        }

        public void OnNewRequest(RequestEvent requestEvent)
        {
            var request = requestEvent.Request;
            AddRequestBlock(requestEvent, "New Request",
                $"Client: {request.Client}",
                $"Content: {request.Request.Content.Cid}",
                $"Duration: {BigIntToDuration(request.Request.Ask.Duration)}",
                $"Expiry: {BigIntToDuration(request.Request.Expiry)}",
                $"Collateral: {BitIntToTestTokens(request.Request.Ask.Collateral)}",
                $"Reward: {BitIntToTestTokens(request.Request.Ask.Reward)}",
                $"Number of Slots: {request.Request.Ask.Slots}",
                $"Slot Tolerance: {request.Request.Ask.MaxSlotLoss}",
                $"Slot Size: {BigIntToByteSize(request.Request.Ask.SlotSize)}"
            );
        }

        public void OnRequestCancelled(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, "Cancelled");
        }

        public void OnRequestFinished(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, "Finished");
        }

        public void OnRequestFulfilled(RequestEvent requestEvent)
        {
            AddRequestBlock(requestEvent, "Started");
        }

        public void OnSlotFilled(RequestEvent requestEvent, EthAddress host, BigInteger slotIndex)
        {
            AddRequestBlock(requestEvent, "Slot Filled",
                $"Host: {host}",
                $"Slot Index: {slotIndex}"
            );
        }

        public void OnSlotFreed(RequestEvent requestEvent, BigInteger slotIndex)
        {
            AddRequestBlock(requestEvent, "Slot Freed",
                $"Slot Index: {slotIndex}"
            );
        }

        private void AddRequestBlock(RequestEvent requestEvent, string eventName, params string[] content)
        {
            var blockNumber = $"[{requestEvent.Block.BlockNumber}]";
            var title = $"{blockNumber} **{eventName}** `{requestEvent.Request.Request.Id}`";
            AddBlock(title, content);
        }

        private void AddBlock(string title, params string[] content)
        {
            events.Add(FormatBlock(title, content));
        }

        private string FormatBlock(string title, params string[] content)
        {
            if (content == null || !content.Any())
            {
                return $"{title}{nl}{nl}";
            }

            return string.Join(nl,
                new string[]
                {
                    title,
                    "```"
                }
                .Concat(content)
                .Concat(new string[]
                {
                    "```"
                })
            ) + nl + nl;
        }

        private string BigIntToDuration(BigInteger big)
        {
            var span = TimeSpan.FromSeconds((int)big);
            return Time.FormatDuration(span);
        }

        private string BigIntToByteSize(BigInteger big)
        {
            var size = new ByteSize((long)big);
            return size.ToString();
        }

        private string BitIntToTestTokens(BigInteger big)
        {
            var tt = new TestToken(big);
            return tt.ToString();
        }
    }
}
