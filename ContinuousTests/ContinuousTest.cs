using DistTestCore;
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
        protected const int Zero = 0;
        protected const int MinuteOne = 60;
        protected const int MinuteFive = MinuteOne * 5;
        protected const int HourOne = MinuteOne * 60;
        protected const int HourThree = HourOne * 3;
        protected const int DayOne = HourOne * 24;
        protected const int DayThree = DayOne * 3;

        private const string UploadFailedMessage = "Unable to store block";

        public void Initialize(CodexNode[] nodes, BaseLog log, FileManager fileManager, Configuration configuration, CancellationToken cancelToken)
        {
            Nodes = nodes;
            Log = log;
            FileManager = fileManager;
            Configuration = configuration;
            CancelToken = cancelToken;

            if (nodes != null)
            {
                NodeRunner = new NodeRunner(Nodes, configuration, TimeSet, Log, CustomK8sNamespace, EthereumAccountIndex);
            }
            else
            {
                NodeRunner = null!;
            }
        }

        public CodexNode[] Nodes { get; private set; } = null!;
        public BaseLog Log { get; private set; } = null!;
        public IFileManager FileManager { get; private set; } = null!;
        public Configuration Configuration { get; private set; } = null!;
        public virtual ITimeSet TimeSet { get { return new DefaultTimeSet(); } }
        public CancellationToken CancelToken { get; private set; } = null;
        public NodeRunner NodeRunner { get; private set; } = null!;

        public abstract int RequiredNumberOfNodes { get; }
        public abstract TimeSpan RunTestEvery { get; }
        public abstract TestFailMode TestFailMode { get; }
        public virtual int EthereumAccountIndex { get { return -1; } }
        public virtual string CustomK8sNamespace { get { return string.Empty; } }

        public string Name
        {
            get
            {
                return GetType().Name;
            }
        }

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

        public TestFile DownloadFile(CodexNode node, ContentId contentId, string fileLabel = "")
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

    public enum TestFailMode
    {
        StopAfterFirstFailure,
        AlwaysRunAllMoments
    }
}
