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
        private readonly object _lock = new object();
        private readonly string transcriptFile;
        private readonly string artifactsFolder;
        private readonly Dictionary<string, string> header = new Dictionary<string, string>();
        private readonly SortedList<DateTime, List<OverwatchEvent>> buffer = new SortedList<DateTime, List<OverwatchEvent>>();
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

            var newEvent = new OverwatchEvent
            {
                Type = typeName,
                Payload = JsonConvert.SerializeObject(payload)
            };

            lock (_lock)
            {
                if (buffer.ContainsKey(utc))
                {
                    buffer[utc].Add(newEvent);
                }
                else
                {
                    buffer.Add(utc, new List<OverwatchEvent> { newEvent });
                }
            }
        }

        public void AddHeader(string key, object value)
        {
            CheckClosed();
            lock (_lock)
            {
                header.Add(key, JsonConvert.SerializeObject(value));
            }
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
            if (!buffer.Any()) throw new Exception("No entries added.");

            CheckClosed();
            closed = true;

            var model = CreateModel();

            File.WriteAllText(transcriptFile, JsonConvert.SerializeObject(model, Formatting.Indented));

            ZipFile.CreateFromDirectory(workingDir, outputFilename);

            Directory.Delete(workingDir, true);
        }

        private OverwatchTranscript CreateModel()
        {
            lock (_lock)
            {
                var model = new OverwatchTranscript
                {
                    Header = new OverwatchHeader
                    {
                        Common = CreateCommonHeader(),
                        Entries = header.Select(h =>
                        {
                            return new OverwatchHeaderEntry
                            {
                                Key = h.Key,
                                Value = h.Value
                            };
                        }).ToArray()
                    },
                    Moments = buffer.Select(p =>
                    {
                        return new OverwatchMoment
                        {
                            Utc = p.Key,
                            Events = p.Value.ToArray()
                        };
                    }).ToArray()
                };

                header.Clear();
                buffer.Clear();

                return model;
            }
        }

        private OverwatchCommonHeader CreateCommonHeader()
        {
            return new OverwatchCommonHeader
            {
                NumberOfEvents = buffer.Sum(e => e.Value.Count),
                EarliestUct = buffer.Min(e => e.Key),
                LatestUtc = buffer.Max(e => e.Key)
            };
        }

        private void CheckClosed()
        {
            if (closed) throw new Exception("Transcript has already been written. Cannot modify or write again.");
        }
    }
}
