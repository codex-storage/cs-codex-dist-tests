using CodexClient;

namespace CodexPlugin.OverwatchSupport.LineConverters
{
    public class BlockReceivedLineConverter : ILineConverter
    {
        public string Interest => "Received blocks from peer";

        public void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent)
        {
            var peer = line.Attributes["peer"];
            var blockAddresses = line.Attributes["blocks"];

            SplitBlockAddresses(blockAddresses, address =>
            {
                addEvent(e =>
                {
                    e.BlockReceived = new BlockReceivedEvent
                    {
                        SenderPeerId = peer,
                        BlockAddress = address
                    };
                });
            });
        }

        private void SplitBlockAddresses(string blockAddresses, Action<string> onBlockAddress)
        {
            // Single line can contain multiple block addresses.
            var tokens = blockAddresses.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            while (tokens.Count > 0)
            {
                if (tokens.Count == 1)
                {
                    onBlockAddress(tokens[0]);
                    return;
                }

                var blockAddress = $"{tokens[0]}, {tokens[1]}";
                tokens.RemoveRange(0, 2);

                onBlockAddress(blockAddress);
            }
        }
    }
}
