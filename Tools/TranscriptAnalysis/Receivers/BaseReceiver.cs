using CodexPlugin.OverwatchSupport;
using Logging;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public abstract class BaseReceiver<T> : IEventReceiver<T>
    {
        protected ILog log { get; private set; } = new NullLog();
        protected OverwatchCodexHeader Header { get; private set; } = null!;

        public abstract string Name { get; }
        public abstract void Receive(ActivateEvent<T> @event);
        public abstract void Finish();

        public void Init(ILog log, OverwatchCodexHeader header)
        {
            this.log = new LogPrefixer(log, $"({Name}) ");
            Header = header;
        }

        protected string? GetPeerId(int nodeIndex)
        {
            return GetIdentity(nodeIndex)?.PeerId;
        }

        protected string? GetName(int nodeIndex)
        {
            return GetIdentity(nodeIndex)?.Name;
        }

        protected CodexNodeIdentity? GetIdentity(int nodeIndex)
        {
            if (nodeIndex < 0) return null;
            return Header.Nodes[nodeIndex];
        }

        protected void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
