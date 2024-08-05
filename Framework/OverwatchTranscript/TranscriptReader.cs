using Newtonsoft.Json;
using System.IO;
using System;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace OverwatchTranscript
{
    public interface ITranscriptReader
    {
        OverwatchCommonHeader Header { get; }
        T GetHeader<T>(string key);
        void AddMomentHandler(Action<ActivateMoment> handler);
        void AddEventHandler<T>(Action<ActivateEvent<T>> handler);
        bool Next();
        void Close();
    }

    public class TranscriptReader : ITranscriptReader
    {
        private readonly object handlersLock = new object();
        private readonly string transcriptFile;
        private readonly string artifactsFolder;
        private readonly List<Action<ActivateMoment>> momentHandlers = new List<Action<ActivateMoment>>();
        private readonly Dictionary<string, List<Action<ActivateMoment, string>>> eventHandlers = new Dictionary<string, List<Action<ActivateMoment, string>>>();
        private readonly string workingDir;
        private readonly OverwatchTranscript model;
        private bool closed;
        private long momentCounter;
        private readonly ConcurrentQueue<OverwatchMoment> queue = new ConcurrentQueue<OverwatchMoment>();
        private readonly Task queueFiller;

        public TranscriptReader(string workingDir, string inputFilename)
        {
            closed = false;
            this.workingDir = workingDir;
            transcriptFile = Path.Combine(workingDir, TranscriptConstants.TranscriptFilename);
            artifactsFolder = Path.Combine(workingDir, TranscriptConstants.ArtifactFolderName);

            if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
            if (File.Exists(transcriptFile) || Directory.Exists(artifactsFolder)) throw new Exception("workingdir not clean");

            model = LoadModel(inputFilename);

            queueFiller = Task.Run(() => FillQueue(model, workingDir));
        }

        public OverwatchCommonHeader Header
        {
            get
            {
                CheckClosed();
                return model.Header.Common;
            }
        }

        public T GetHeader<T>(string key)
        {
            CheckClosed();
            var value = model.Header.Entries.First(e => e.Key == key).Value;
            return JsonConvert.DeserializeObject<T>(value)!;
        }

        public void AddMomentHandler(Action<ActivateMoment> handler)
        {
            CheckClosed();
            lock (handlersLock)
            {
                momentHandlers.Add(handler);
            }
        }

        public void AddEventHandler<T>(Action<ActivateEvent<T>> handler)
        {
            CheckClosed();

            var typeName = typeof(T).FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("Empty typename for payload");

            lock (handlersLock)
            {
                if (eventHandlers.ContainsKey(typeName))
                {
                    eventHandlers[typeName].Add(CreateEventAction(handler));
                }
                else
                {
                    eventHandlers.Add(typeName, new List<Action<ActivateMoment, string>>
                    {
                        CreateEventAction(handler)
                    });
                }
            }
        }

        private readonly object nextLock = new object();
        private OverwatchMoment? moment = null;
        private OverwatchMoment? next = null;

        public bool Next()
        {
            CheckClosed();

            OverwatchMoment? m = null;
            TimeSpan? duration = null;
            lock (nextLock)
            {
                if (next == null)
                {
                    if (!queue.TryDequeue(out moment)) return false;
                    queue.TryDequeue(out next);
                }
                else
                {
                    moment = next;
                    next = null;
                    queue.TryDequeue(out next);
                }

                m = moment;
                duration = GetMomentDuration();
            }

            ActivateMoment(moment, duration);

            return true;
        }

        public void Close()
        {
            CheckClosed();
            closed = true;

            queueFiller.Wait();

            Directory.Delete(workingDir, true);
        }

        private Action<ActivateMoment, string> CreateEventAction<T>(Action<ActivateEvent<T>> handler)
        {
            return (m, s) =>
            {
                handler(new ActivateEvent<T>(m, JsonConvert.DeserializeObject<T>(s)!));
            };
        }

        private void FillQueue(OverwatchTranscript model, string workingDir)
        {
            var reader = new MomentReader(model, workingDir);

            while (true)
            {
                if (closed)
                {
                    reader.Close();
                    return;
                }

                while (queue.Count < 10)
                {
                    var moment = reader.Next();
                    if (moment == null)
                    {
                        reader.Close();
                        return;
                    }
                    queue.Enqueue(moment);
                }

                Thread.Sleep(1);
            }
        }

        private TimeSpan? GetMomentDuration()
        {
            if (moment == null) return null;
            if (next == null) return null;

            return next.Utc - moment.Utc;
        }

        private void ActivateMoment(OverwatchMoment moment, TimeSpan? duration)
        {
            var m = new ActivateMoment(moment.Utc, duration, momentCounter);

            lock (handlersLock)
            {
                ActivateMomentHandlers(m);

                foreach (var @event in moment.Events)
                {
                    ActivateEventHandlers(m, @event);
                }
            }

            momentCounter++;
        }

        private void ActivateMomentHandlers(ActivateMoment m)
        {
            foreach (var handler in momentHandlers)
            {
                handler(m);
            }
        }

        private void ActivateEventHandlers(ActivateMoment m, OverwatchEvent @event)
        {
            if (!eventHandlers.ContainsKey(@event.Type)) return;
            var handlers = eventHandlers[@event.Type];

            foreach (var handler in handlers)
            {
                handler(m, @event.Payload);
            }
        }

        private OverwatchTranscript LoadModel(string inputFilename)
        {
            ZipFile.ExtractToDirectory(inputFilename, workingDir);

            if (!File.Exists(transcriptFile))
            {
                closed = true;
                throw new Exception("Is not a transcript file. Unzipped to: " + workingDir);
            }

            return JsonConvert.DeserializeObject<OverwatchTranscript>(File.ReadAllText(transcriptFile))!;
        }

        private void CheckClosed()
        {
            if (closed) throw new Exception("Transcript has already been closed.");
        }
    }

    public class ActivateMoment
    {
        public ActivateMoment(DateTime utc, TimeSpan? duration, long index)
        {
            Utc = utc;
            Duration = duration;
            Index = index;
        }

        public DateTime Utc { get; }
        public TimeSpan? Duration { get; }
        public long Index { get; }
    }

    public class ActivateEvent<T>
    {
        public ActivateEvent(ActivateMoment moment, T payload)
        {
            Moment = moment;
            Payload = payload;
        }

        public ActivateMoment Moment { get; }
        public T Payload { get; }
    }
}
