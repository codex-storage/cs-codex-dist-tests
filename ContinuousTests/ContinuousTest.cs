﻿using DistTestCore;
using DistTestCore.Codex;
using Logging;

namespace ContinuousTests
{
    public abstract class ContinuousTestLongTimeouts : ContinuousTest
    {
        public override ITimeSet TimeSet => new LongTimeSet();
    }

    public abstract class ContinuousTest
    {
        private const string UploadFailedMessage = "Unable to store block";

        public void Initialize(CodexNode[] nodes, BaseLog log, FileManager fileManager)
        {
            Nodes = nodes;
            Log = log;
            FileManager = fileManager;
        }

        public CodexNode[] Nodes { get; private set; } = null!;
        public BaseLog Log { get; private set; } = null!;
        public IFileManager FileManager { get; private set; } = null!;
        public virtual ITimeSet TimeSet { get { return new DefaultTimeSet(); } }

        public abstract int RequiredNumberOfNodes { get; }

        public string Name
        {
            get
            {
                return GetType().Name;
            }
        }

        public abstract void Run();

        public ContentId? UploadFile(CodexNode node, TestFile file)
        {
            using var fileStream = File.OpenRead(file.Filename);

            var logMessage = $"Uploading file {file.Describe()}...";
            var response = Stopwatch.Measure(Log, logMessage, () =>
            {
                return node.UploadFile(fileStream);
            });

            if (response.StartsWith(UploadFailedMessage))
            {
                return null;
            }
            Log.Log($"Uploaded file. Received contentId: '{response}'.");
            return new ContentId(response);
        }

        public TestFile DownloadContent(CodexNode node, ContentId contentId, string fileLabel = "")
        {
            var logMessage = $"Downloading for contentId: '{contentId.Id}'...";
            var file = FileManager.CreateEmptyTestFile(fileLabel);
            Stopwatch.Measure(Log, logMessage, () => DownloadToFile(node, contentId.Id, file));
            Log.Log($"Downloaded file {file.Describe()} to '{file.Filename}'.");
            return file;
        }

        private void DownloadToFile(CodexNode node, string contentId, TestFile file)
        {
            using var fileStream = File.OpenWrite(file.Filename);
            try
            {
                using var downloadStream = node.DownloadFile(contentId);
                downloadStream.CopyTo(fileStream);
            }
            catch
            {
                Log.Log($"Failed to download file '{contentId}'.");
                throw;
            }
        }
    }
}