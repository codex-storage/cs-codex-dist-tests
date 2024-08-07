using Logging;
using OverwatchTranscript;

namespace TranscriptAnalysis.Receivers
{
    public abstract class BaseReceiver<T> : IEventReceiver<T>
    {
        protected ILog log { get; private set; } = new NullLog();

        public abstract string Name { get; }
        public abstract void Receive(ActivateEvent<T> @event);
        public abstract void Finish();

        public void Init(ILog log)
        {
            this.log = new LogPrefixer(log, $"({Name}) ");
        }

        protected void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
