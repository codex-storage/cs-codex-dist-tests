using CodexPlugin;
using System.IO;
using System.Windows.Media;

namespace SeeTimeline
{
    public class CodexEvent
    {
        public string Name { get; }
        public Color Color { get; }
        public DateTime Dt { get; private set; }

        public CodexEvent(string name, Color color, DateTime dt)
        {
            Name = name;
            Color = color;
            Dt = dt;
        }

        public void Scale(DateTime from, double factor)
        {
            var span = Dt - from;
            Dt = from + (span * factor);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class EventSet
    {
        public const string NoAddress = "-";
        public const string MiscAddress = "~";

        private readonly Dictionary<string, List<CodexEvent>> events = new Dictionary<string, List<CodexEvent>>();
        private readonly LogLineAdder adder = new LogLineAdder();

        public void Add(string address, CodexEvent e)
        {
            if (!events.ContainsKey(address)) events.Add(address, new List<CodexEvent>());
            events[address].Add(e);

            if (e.Dt > Latest) Latest = e.Dt;
            if (e.Dt < Earliest) Earliest = e.Dt;
        }

        public string[] Addresses => events.Keys.ToArray();
        public DateTime Earliest { get; private set; } = DateTime.MaxValue;
        public DateTime Latest { get; private set; } = DateTime.MinValue;

        public void KeepOnly(string[] addresses)
        {
            var keys = events.Keys.ToArray();
            foreach (var key in keys)
            {
                if (key != NoAddress && !key.StartsWith(MiscAddress) && !addresses.Contains(key))
                {
                    events.Remove(key);
                }
            }
        }

        public void Iterate(Action<string, CodexEvent[]> action)
        {
            foreach (var pair in events)
            {
                action(pair.Key, pair.Value.ToArray());
            }
        }

        public void AddLine(string line)
        {
            var cline = CodexLogLine.Parse(line);
            if (cline == null) return;
            adder.Add(cline, this);
        }

        public void AddFile(string path)
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines) AddLine(line);
        }

        public void Scale(DateTime from, double factor)
        {
            foreach (var pair in events)
            {
                foreach (var e in pair.Value) e.Scale(from, factor);
            }
        }
    }

    public class LogLineAdder
    {
        public void Add(CodexLogLine line, EventSet set)
        {
            var context = new SetLineContext(line, set);
            context.Parse();
        }

        public class SetLineContext
        {
            private readonly CodexLogLine line;
            private readonly EventSet set;

            public SetLineContext(CodexLogLine line, EventSet set)
            {
                this.line = line;
                this.set = set;
            }

            public void Parse()
            {
                var colormap = new Dictionary<string, Color>
                {
                    { "0", Colors.Black },
                    { "1", Colors.Red },
                    { "2", Colors.Green },
                    { "3", Colors.Blue },
                    { "4", Colors.Red },
                    { "5", Colors.Green },
                    { "6", Colors.Blue },
                    { "7", Colors.Red },
                    { "8", Colors.Green },
                    { "9", Colors.Blue },
                    { "10", Colors.Black }
                };

                //if (!line.Attributes.Any(a => a.Value.ToLowerInvariant().Contains("index: 5"))) return;

                //AddJobs(result, "Created", Colors.Red, req.Created);
                // trace "BlockRequest created", address
                if (line.Message == "BlockRequest created") AddEvent(line.Attributes["address"], "ReqCreated", Colors.Red);

                //AddJobs(result, "TaskScheduled", Colors.Purple, req.TaskScheduled);
                //            trace "Task scheduled", peerId = task.id
                else if (line.Message == "Task scheduled") AddEvent(EventSet.NoAddress, "TaskScheduled", Colors.Black);

                //trace "Sending wantHave request", toAsk, peer = p.id
                //AddJobs(result, "WantHaveSent", Colors.Orange, req.WantHaveSent);
                else if (line.Message == "Sending wantHave request") AddMultiple(line.Attributes["toAsk"], "SentWantHave", Colors.Orange);

                //trace "Sending wantBlock request to", addresses, peer = blockPeer.id
                //AddJobs(result, "WantBlkSent", Colors.Green, req.WantBlkSent);
                else if (line.Message == "Sending wantBlock request to") AddMultiple(line.Attributes["addresses"], "SentWantBlk", Colors.Green);

                //trace "Handling blockPresences", addrs = blocks.mapIt(it.address), anyCancel = blocks.anyIt(it.isCancel)
                //AddJobs(result, "PresenceRecv", Colors.Yellow, req.PresenceRecv);
                else if (line.Message == "Handling blockPresences")
                {
                    var anyCancel = line.Attributes["anyCancel"].ToLowerInvariant();
                    if (anyCancel == "true")
                    {
                        AddMultiple(line.Attributes["addrs"], "CancelPresenceRecv", Colors.Red);
                    }
                    else
                    {
                        AddMultiple(line.Attributes["addrs"], "PresenceRecv", Colors.Yellow);
                    }
                }

                //trace "Sending block request cancellations to peers", addrs, peers = b.peers.mapIt($it.id)
                //AddJobs(result, "CancelSent", Colors.Purple, req.CancelSent);
                else if (line.Message == "Sending block request cancellations to peers") AddMultiple(line.Attributes["addrs"], "CancelSent", Colors.Purple);

                //trace "Resolving blocks", addrs = blocksDelivery.mapIt(it.address)
                //AddJobs(result, "Resolve", Colors.Pink, req.Resolve);
                else if (line.Message == "Resolving blocks") AddMultiple(line.Attributes["addrs"], "Resolve", Colors.Pink);

                //trace "Received blocks from peer", peer, blocks = (blocksDelivery.mapIt(it.address))
                //AddJobs(result, "BlkRecv", Colors.Blue, req.BlkRecv);
                else if (line.Message == "Received blocks from peer") AddMultiple(line.Attributes["blocks"], "BlkRecv", Colors.Blue);

                //logScope:
                //            peer = peerCtx.id
                //    address = e.address
                //    wantType = $e.wantType
                //    isCancel = $e.cancel
                //trace "Received wantHave".
                //AddJobs(result, "WantHaveRecv", Colors.Red, req.WantHaveRecv);
                else if (line.Message == "Received wantHave")
                {
                    var isCancel = line.Attributes["isCancel"];

                    if (isCancel.ToLowerInvariant() == "true")
                    {
                        AddEvent(line.Attributes["address"], "CancelRecv", Colors.Red);
                    }
                    else
                    {
                        AddEvent(line.Attributes["address"], "WantHaveRecv", Colors.Red);
                    }
                }

                //trace "Received wantBlock"
                //AddJobs(result, "WantBlkRecv", Colors.Yellow, req.WantBlkRecv);
                else if (line.Message == "Received wantBlock") AddEvent(line.Attributes["address"], "WantBlkRecv", Colors.Yellow);

                //trace "Sending presence to remote", addrs = presence.mapIt(it.address), anyCancel = presence.anyIt(it.isCancel)
                //AddJobs(result, "PresenceSent", Colors.Orange, req.PresenceSent);
                else if (line.Message == "Sending presence")
                {
                    var isCancel = line.Attributes["anyCancel"];

                    if (isCancel.ToLowerInvariant() == "true")
                    {
                        AddMultiple(line.Attributes["addrs"], "CancelPresenceSent", Colors.Red);
                    }
                    else
                    {
                        AddMultiple(line.Attributes["addrs"], "PresenceSent", Colors.Orange);
                    }
                }

                //trace "Begin sending blocks", addrs = wantAddresses
                //AddJobs(result, "BlkSendStart", Colors.Green, req.BlkSendStart);
                else if (line.Message == "Begin sending blocks") AddMultiple(line.Attributes["addrs"], "BlkSendStart", Colors.Green);

                //trace "Finished sending blocks", addrs = wantAddresses
                //AddJobs(result, "BlkSendEnd", Colors.Blue, req.BlkSendEnd);
                else if (line.Message == "Finished sending blocks") AddMultiple(line.Attributes["addrs"], "BlkSendEnd", Colors.Blue);

                else if (line.Message == "tick") AddEvent(EventSet.MiscAddress, "tick", Colors.Black);

                //// Discovery Started
                //else if (line.Message == "Discovery Started") AddEvent(EventSet.NoAddress, "DiscoveryStart", Colors.Black);

                //// Setting up peer
                //else if (line.Message == "Setting up peer") AddEvent(EventSet.NoAddress, "NewPeer", Colors.Black);

                //// Error block handle, disconnecting peer
                //else if (line.Message == "Error block handle, disconnecting peer") AddEvent(EventSet.NoAddress, "DisconnectPeer", Colors.Black);

                //// Dropping peer
                //else if (line.Message == "Dropping peer") AddEvent(EventSet.NoAddress, "DisconnectPeer", Colors.Black);

                //// Acquired slot
                //else if (line.Message == "Acquired slot") AddEvent(EventSet.MiscAddress, "AcqSlot", Colors.Black);

                //// Released slot
                //else if (line.Message == "Released slot") AddEvent(EventSet.MiscAddress, "RlsSlot", Colors.Black);

                //// Got discv5 lookup query response
                //else if (line.Message == "Got discv5 lookup query response") AddEvent(EventSet.MiscAddress, "Discv5", Colors.Black);

                //// waiting for data
                //else if (line.Message == "waiting for data") AddEvent(EventSet.MiscAddress, "wait", Colors.Black);


                // MsgReceived
                // MsgSending
                // MsgSent

                else if (line.Message == "MsgSending") AddEvent(EventSet.MiscAddress + "snd", line.Attributes["num"], colormap[line.Attributes["num"]]);
                else if (line.Message == "MsgSent") AddEvent(EventSet.MiscAddress + "snt", line.Attributes["num"], colormap[line.Attributes["num"]]);
                else if (line.Message == "MsgReceived") AddEvent(EventSet.MiscAddress + "rcv", line.Attributes["num"], colormap[line.Attributes["num"]]);
            }

            private void AddMultiple(string addresses, string name, Color color)
            {
                var addressToken = addresses
                        .Replace("@[", "")
                        .Replace("]", "");

                //foreach (var adddress in addressTokens)
                //{
                //    AddEvent(adddress, name, color);
                //}
                AddEvent(addressToken, name, color);
            }

            private void AddEvent(string address, string name, Color color)
            {
                set.Add(address, new CodexEvent(name, color, line.TimestampUtc));
            }
        }
    }
}
