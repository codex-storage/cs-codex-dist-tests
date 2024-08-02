using Newtonsoft.Json;
using System.IO;
using System;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace OverwatchTranscript
{
    public interface ITranscriptReader
    {
        OverwatchCommonHeader Header { get; }
        T GetHeader<T>(string key);
        void AddMomentHandler(Action<ActivateMoment> handler);
        void AddEventHandler<T>(Action<ActivateEvent<T>> handler);
        void Next();
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
        private readonly MomentReader reader;
        private bool closed;
        private long momentCounter;
        private readonly object queueLock = new object();
        private readonly List<OverwatchMoment> queue = new List<OverwatchMoment>();
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
            reader = new MomentReader(model, workingDir);

            queueFiller = Task.Run(FillQueue);
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

        public void Next()
        {
            CheckClosed();
            OverwatchMoment moment = null!;
            OverwatchMoment? next = null;
            lock (queueLock)
            {
                if (queue.Count == 0) return;
                moment = queue[0];
                if (queue.Count > 1) next = queue[1];

                queue.RemoveAt(0);
            }

            var duration = GetMomentDuration(moment, next);
            ActivateMoment(moment, duration);
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

        private void FillQueue()
        {
            while (true)
            {
                if (closed) return;

                lock (queueLock)
                {
                    while (queue.Count < 100)
                    {
                        var moment = reader.Next();
                        if (moment == null) return;
                        queue.Add(moment);
                    }
                }

                Thread.Sleep(1);
            }
        }

        private TimeSpan? GetMomentDuration(OverwatchMoment moment, OverwatchMoment? next)
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
