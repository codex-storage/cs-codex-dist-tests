using Logging;
using Utils;

namespace FileUtils
{
    public class TrackedFile
    {
        private readonly ILog log;

        public TrackedFile(ILog log, string filename, string label)
        {
            this.log = log;
            Filename = filename;
            Label = label;
        }

        public string Filename { get; }
        public string Label { get; }

        public void AssertIsEqual(TrackedFile? actual)
        {
            var sw = Stopwatch.Begin(log);
            try
            {
                AssertEqual(actual);
            }
            finally
            {
                sw.End($"{nameof(TrackedFile)}.{nameof(AssertIsEqual)}");
            }
        }

        public string Describe()
        {
            var sizePostfix = $" ({Formatter.FormatByteSize(GetFileSize())})";
            if (!string.IsNullOrEmpty(Label)) return Label + sizePostfix;
            return $"'{Filename}'{sizePostfix}";
        }

        public ByteSize GetFilesize()
        {
            return new ByteSize(GetFileSize());
        }

        private void AssertEqual(TrackedFile? actual)
        {
            if (actual == null)  FrameworkAssert.Fail("TestFile is null.");
            if (actual == this || actual!.Filename == Filename) FrameworkAssert.Fail("TestFile is compared to itself.");

            FrameworkAssert.That(actual.GetFileSize() == GetFileSize(), "Files are not of equal length.");

            using var streamExpected = new FileStream(Filename, FileMode.Open, FileAccess.Read);
            using var streamActual = new FileStream(actual.Filename, FileMode.Open, FileAccess.Read);

            var bytesExpected = new byte[FileManager.ChunkSize];
            var bytesActual = new byte[FileManager.ChunkSize];

            var readExpected = 0;
            var readActual = 0;

            while (true)
            {
                readExpected = streamExpected.Read(bytesExpected, 0, FileManager.ChunkSize);
                readActual = streamActual.Read(bytesActual, 0, FileManager.ChunkSize);

                if (readExpected == 0 && readActual == 0)
                {
                    log.Log($"OK: {Describe()} is equal to {actual.Describe()}.");
                    return;
                }

                FrameworkAssert.That(readActual == readExpected, "Unable to read buffers of equal length.");

                for (var i = 0; i < readActual; i++)
                {
                    if (bytesExpected[i] != bytesActual[i]) FrameworkAssert.Fail("File contents not equal.");
                }
            }
        }

        private long GetFileSize()
        {
            var info = new FileInfo(Filename);
            return info.Length;
        }
    }
}
