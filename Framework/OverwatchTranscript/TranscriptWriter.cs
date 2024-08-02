using Logging;
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
        private readonly MomentReferenceBuilder builder;
        private readonly string transcriptFile;
        private readonly string artifactsFolder;
        private readonly Dictionary<string, string> header = new Dictionary<string, string>();
        private readonly BucketSet bucketSet;
        private readonly ILog log;
        private readonly string workingDir;
        private bool closed;

        public TranscriptWriter(ILog log, string workingDir)
        {
            closed = false;
            this.log = log;
            this.workingDir = workingDir;
            bucketSet = new BucketSet(log, workingDir);
            builder = new MomentReferenceBuilder(log, workingDir);
            transcriptFile = Path.Combine(workingDir, TranscriptConstants.TranscriptFilename);
            artifactsFolder = Path.Combine(workingDir, TranscriptConstants.ArtifactFolderName);

            if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
            if (File.Exists(transcriptFile) || Directory.Exists(artifactsFolder)) throw new Exception("workingdir not clean");
        }

        public void Add(DateTime utc, object payload)
        {
            CheckClosed();
            bucketSet.Add(utc, payload);
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
            CheckClosed();
            closed = true;

            var momentReferences = builder.Build(bucketSet.FinalizeBuckets());
            var model = CreateModel(momentReferences);

            File.WriteAllText(transcriptFile, JsonConvert.SerializeObject(model, Formatting.Indented));

            ZipFile.CreateFromDirectory(workingDir, outputFilename);
            log.Debug($"Transcript written to {outputFilename}");
            log.Debug($"Common header: {JsonConvert.SerializeObject(model.Header.Common, Formatting.Indented)}");

            Directory.Delete(workingDir, true);
            log.Debug($"Workdir {workingDir} deleted");
        }

        private OverwatchTranscript CreateModel(OverwatchMomentReference[] momentReferences)
        {
            lock (_lock)
            {
                var model = new OverwatchTranscript
                {
                    Header = new OverwatchHeader
                    {
                        Common = CreateCommonHeader(momentReferences),
                        Entries = header.Select(h =>
                        {
                            return new OverwatchHeaderEntry
                            {
                                Key = h.Key,
                                Value = h.Value
                            };
                        }).ToArray()
                    },
                    MomentReferences = momentReferences
                };

                header.Clear();
                return model;
            }
        }

        private OverwatchCommonHeader CreateCommonHeader(OverwatchMomentReference[] momentReferences)
        {
            var moments = momentReferences.Sum(m => m.NumberOfMoments);
            var events = momentReferences.Sum(m => m.NumberOfEvents);
            var earliest = momentReferences.Min(m => m.EarliestUtc);
            var latest = momentReferences.Max(m => m.LatestUtc);

            return new OverwatchCommonHeader
            {
                NumberOfMoments = moments,
                NumberOfEvents = events,
                EarliestUtc = earliest,
                LatestUtc = latest
            };
        }

        private void CheckClosed()
        {
            if (closed) throw new Exception("Transcript has already been written. Cannot modify or write again.");
        }
    }
}
