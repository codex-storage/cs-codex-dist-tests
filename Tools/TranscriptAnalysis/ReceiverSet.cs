using Logging;
using OverwatchTranscript;
using TranscriptAnalysis.Receivers;

namespace TranscriptAnalysis
{
    public interface IEventReceiver
    {
        void Init(ILog log);
        void Finish();
    }

    public interface IEventReceiver<T> : IEventReceiver
    {
        void Receive(ActivateEvent<T> @event);
    }

    public class ReceiverSet
    {
        private readonly ILog log;
        private readonly ITranscriptReader reader;
        private readonly List<IEventReceiver> receivers = new List<IEventReceiver>();

        public ReceiverSet(ILog log, ITranscriptReader reader)
        {
            this.log = log;
            this.reader = reader;
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
            receiver.Init(log);
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
