using Newtonsoft.Json;
using System.IO.Compression;

namespace OverwatchTranscript
{
    public interface ITranscriptWriter
    {
        void AddHeader(string key, object value);
        void Add(DateTime utc, object payload);
        void IncludeArtifact(string filePath);
        void Write(string outputFilename);
    }

    public class TranscriptWriter : ITranscriptWriter
    {
        private readonly string transcriptFile;
        private readonly string artifactsFolder;
        private readonly Dictionary<string, string> header = new Dictionary<string, string>();
        private readonly SortedList<DateTime, OverwatchEvent> buffer = new SortedList<DateTime, OverwatchEvent>();
        private readonly string workingDir;
        private bool closed;

        public TranscriptWriter(string workingDir)
        {
            closed = false;
            this.workingDir = workingDir;
            transcriptFile = Path.Combine(workingDir, TranscriptConstants.TranscriptFilename);
            artifactsFolder = Path.Combine(workingDir, TranscriptConstants.ArtifactFolderName);

            if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
            if (File.Exists(transcriptFile) || Directory.Exists(artifactsFolder)) throw new Exception("workingdir not clean");
        }

        public void Add(DateTime utc, object payload)
        {
            CheckClosed();
            var typeName = payload.GetType().FullName;
            if (string.IsNullOrEmpty(typeName)) throw new Exception("Empty typename for payload");

            buffer.Add(utc, new OverwatchEvent
            {
                Utc = utc,
                Type = typeName,
                Payload = JsonConvert.SerializeObject(payload)
            });
        }

        public void AddHeader(string key, object value)
        {
            CheckClosed();
            header.Add(key, JsonConvert.SerializeObject(value));
        }

        public void IncludeArtifact(string filePath)
        {
            CheckClosed();
            if (!File.Exists(filePath)) throw new Exception("File not found: " + filePath);
            if (!Directory.Exists(artifactsFolder)) Directory.CreateDirectory(artifactsFolder);
            var name = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(artifactsFolder, name), overwrite: false);
        }

        public void Write(string outputFilename)
        {
            CheckClosed();
            closed = true;

            var model = new OverwatchTranscript
            {
                Header = new OverwatchHeader
                {
                    Entries = header.Select(h =>
                    {
                        return new OverwatchHeaderEntry
                        {
                            Key = h.Key,
                            Value = h.Value
                        };
                    }).ToArray()
                },
                Events = buffer.Values.ToArray()
            };

            header.Clear();
            buffer.Clear();

            File.WriteAllText(transcriptFile, JsonConvert.SerializeObject(model, Formatting.Indented));

            ZipFile.CreateFromDirectory(workingDir, outputFilename);

            Directory.Delete(workingDir, true);
        }

        private void CheckClosed()
        {
            if (closed) throw new Exception("Transcript has already been written. Cannot modify or write again.");
        }
    }
}
