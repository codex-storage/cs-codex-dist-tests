using CodexPlugin.OverwatchSupport;
using Logging;
using OverwatchTranscript;
using TranscriptAnalysis.Receivers;

namespace TranscriptAnalysis
{
    public interface IEventReceiver
    {
        void Init(string sourceFilename, ILog log, OverwatchCodexHeader header);
        void Finish();
    }

    public interface IEventReceiver<T> : IEventReceiver
    {
        void Receive(ActivateEvent<T> @event);
    }

    public class ReceiverSet
    {
        private readonly string sourceFilename;
        private readonly ILog log;
        private readonly ITranscriptReader reader;
        private readonly OverwatchCodexHeader header;
        private readonly List<IEventReceiver> receivers = new List<IEventReceiver>();

        public ReceiverSet(string sourceFilename, ILog log, ITranscriptReader reader, OverwatchCodexHeader header)
        {
            this.sourceFilename = sourceFilename;
            this.log = log;
            this.reader = reader;
            this.header = header;
        }

        public void InitAll()
        {
            Add(new LogReplaceReceiver());
            Add(new DuplicateBlocksReceived());
            Add(new NodesDegree());
        }

        public void FinishAll()
        {
            foreach (var r in receivers)
            {
                r.Finish();
            }
            receivers.Clear();
        }


        private void Add<T>(IEventReceiver<T> receiver)
        {
            var mux = GetMux<T>();
            mux.Add(receiver);

            receivers.Add(receiver);
            receiver.Init(sourceFilename, log, header);
        }

        // We use a mux here because, for each time we call reader.AddEventHandler,
        // The reader will perform one separate round of JSON deserialization.
        // TODO: Move the mux into the reader.
        private readonly Dictionary<string, IEventMux> muxes = new Dictionary<string, IEventMux>();
        private IEventMux GetMux<T>()
        {
            var typeName = typeof(T).FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("A!");

            if (!muxes.ContainsKey(typeName))
            {
                muxes.Add(typeName, new EventMux<T>(reader));
            }
            return muxes[typeName];
        }
    }

    public interface IEventMux
    {
        void Add(IEventReceiver receiver);
    }

    public class EventMux<T> : IEventMux
    {
        private readonly List<IEventReceiver<T>> receivers = new List<IEventReceiver<T>>();

        public EventMux(ITranscriptReader reader)
        {
            reader.AddEventHandler<T>(Handle);
        }

        public void Add(IEventReceiver receiver)
        {
            if (receiver is IEventReceiver<T> r)
            {
                receivers.Add(r);
            }
        }

        public void Handle(ActivateEvent<T> @event)
        {
            foreach (var r in receivers) r.Receive(@event);
        }
    }
}
