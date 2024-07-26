using Newtonsoft.Json;
using System.IO.Compression;

namespace OverwatchTranscript
{
    public interface ITranscriptReader
    {
        T GetHeader<T>(string key);
        void AddHandler<T>(Action<DateTime, T> handler);
        void Next();
        void Close();
    }

    public class TranscriptReader : ITranscriptReader
    {
        private readonly string transcriptFile;
        private readonly string artifactsFolder;
        private readonly Dictionary<string, Action<DateTime, string>> handlers = new Dictionary<string, Action<DateTime, string>>();
        private readonly string workingDir;
        private OverwatchTranscript model = null!;
        private int momentIndex = 0;
        private bool closed;

        public TranscriptReader(string workingDir, string inputFilename)
        {
            closed = false;
            this.workingDir = workingDir;
            transcriptFile = Path.Combine(workingDir, TranscriptConstants.TranscriptFilename);
            artifactsFolder = Path.Combine(workingDir, TranscriptConstants.ArtifactFolderName);

            if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
            if (File.Exists(transcriptFile) || Directory.Exists(artifactsFolder)) throw new Exception("workingdir not clean");

            LoadModel(inputFilename);
        }

        public T GetHeader<T>(string key)
        {
            CheckClosed();
            var value = model.Header.Entries.First(e => e.Key == key).Value;
            return JsonConvert.DeserializeObject<T>(value)!;
        }

        public void AddHandler<T>(Action<DateTime, T> handler)
        {
            CheckClosed();
            var typeName = typeof(T).FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("Empty typename for payload");

            handlers.Add(typeName, (utc, s) =>
            {
                handler(utc, JsonConvert.DeserializeObject<T>(s)!);
            });
        }

        public void Next()
        {
            CheckClosed();
            if (momentIndex >= model.Moments.Length) return;

            var moment = model.Moments[momentIndex];
            momentIndex++;

            PlayMoment(moment);
        }

        public void Close()
        {
            CheckClosed();
            Directory.Delete(workingDir, true);
            closed = true;
        }

        private void PlayMoment(OverwatchMoment moment)
        {
            foreach (var @event in moment.Events)
            {
                PlayEvent(moment.Utc, @event);
            }
        }

        private void PlayEvent(DateTime utc, OverwatchEvent @event)
        {
            if (!handlers.ContainsKey(@event.Type)) return;
            var handler = handlers[@event.Type];

            handler(utc, @event.Payload);
        }

        private void LoadModel(string inputFilename)
        {
            ZipFile.ExtractToDirectory(inputFilename, workingDir);

            if (!File.Exists(transcriptFile))
            {
                closed = true;
                throw new Exception("Is not a transcript file. Unzipped to: " + workingDir);
            }

            model = JsonConvert.DeserializeObject<OverwatchTranscript>(File.ReadAllText(transcriptFile))!;
        }

        private void CheckClosed()
        {
            if (closed) throw new Exception("Transcript has already been written. Cannot modify or write again.");
        }
    }
}
