using Logging;
using NUnit.Framework;
using System.Runtime.InteropServices;
using Utils;

namespace DistTestCore
{
    public interface IFileManager
    {
        TestFile CreateEmptyTestFile(string label = "");
        TestFile GenerateTestFile(ByteSize size, string label = "");
        void DeleteAllTestFiles();
        void PushFileSet();
        void PopFileSet();
    }

    public class FileManager : IFileManager
    {
        public const int ChunkSize = 1024 * 1024 * 100;
        private static NumberSource folderNumberSource = new NumberSource(0);
        private readonly Random random = new Random();
        private readonly BaseLog log;
        private readonly string folder;
        private readonly List<List<TestFile>> fileSetStack = new List<List<TestFile>>();

        public FileManager(BaseLog log, Configuration configuration)
        {
            folder = Path.Combine(configuration.GetFileManagerFolder(), folderNumberSource.GetNextNumber().ToString("D5"));

            EnsureDirectory();
            this.log = log;
        }

        public TestFile CreateEmptyTestFile(string label = "")
        {
            var path = Path.Combine(folder, Guid.NewGuid().ToString() + "_test.bin");
            var result = new TestFile(log, path, label);
            File.Create(result.Filename).Close();
            if (fileSetStack.Any()) fileSetStack.Last().Add(result);
            return result;
        }

        public TestFile GenerateTestFile(ByteSize size, string label)
        {
            var sw = Stopwatch.Begin(log);
            var result = GenerateFile(size, label);
            sw.End($"Generated file '{result.Describe()}'.");
            return result;
        }

        public void DeleteAllTestFiles()
        {
            DeleteDirectory();
        }

        public void PushFileSet()
        {
            fileSetStack.Add(new List<TestFile>());
        }

        public void PopFileSet()
        {
            if (!fileSetStack.Any()) return;
            var pop = fileSetStack.Last();
            fileSetStack.Remove(pop);

            foreach (var file in pop)
            {
                try
                {
                    File.Delete(file.Filename);
                }
                catch { }
            }
        }

        private TestFile GenerateFile(ByteSize size, string label)
        {
            var result = CreateEmptyTestFile(label);
            CheckSpaceAvailable(result, size);

            GenerateFileBytes(result, size);
            return result;
        }

        private void CheckSpaceAvailable(TestFile testFile, ByteSize size)
        {
            var file = new FileInfo(testFile.Filename);
            var drive = new DriveInfo(file.Directory!.Root.FullName);

            var spaceAvailable = drive.TotalFreeSpace;

            if (spaceAvailable < size.SizeInBytes)
            {
                var msg = $"Inconclusive: Not enough disk space to perform test. " +
                    $"{Formatter.FormatByteSize(size.SizeInBytes)} required. " +
                    $"{Formatter.FormatByteSize(spaceAvailable)} available.";

                log.Log(msg);
                Assert.Inconclusive(msg);
            }
        }

        private void GenerateFileBytes(TestFile result, ByteSize size)
        {
            long bytesLeft = size.SizeInBytes;
            int chunkSize = ChunkSize;
            while (bytesLeft > 0)
            {
                try
                {
                    var length = Math.Min(bytesLeft, chunkSize);
                    AppendRandomBytesToFile(result, length);
                    bytesLeft -= length;
                }
                catch
                {
                    chunkSize = chunkSize / 2;
                    if (chunkSize < 1024) throw;
                }
            }
        }

        private void AppendRandomBytesToFile(TestFile result, long length)
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);
            using var stream = new FileStream(result.Filename, FileMode.Append);
            stream.Write(bytes, 0, bytes.Length);
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        private void DeleteDirectory()
        {
            Directory.Delete(folder, true);
        }
    }

    public class TestFile
    {
        private readonly BaseLog log;

        public TestFile(BaseLog log, string filename, string label)
        {
            this.log = log;
            Filename = filename;
            Label = label;
        }

        public string Filename { get; }
        public string Label { get; }

        public void AssertIsEqual(TestFile? actual)
        {
            var sw = Stopwatch.Begin(log);
            try
            {
                AssertEqual(actual);
            }
            finally
            {
                sw.End($"{nameof(TestFile)}.{nameof(AssertIsEqual)}");
            }
        }

        public string Describe()
        {
            var sizePostfix = $" ({Formatter.FormatByteSize(GetFileSize())})";
            if (!string.IsNullOrEmpty(Label)) return Label + sizePostfix;
            return $"'{Filename}'{sizePostfix}";
        }

        private void AssertEqual(TestFile? actual)
        {
            if (actual == null) Assert.Fail("TestFile is null.");
            if (actual == this || actual!.Filename == Filename) Assert.Fail("TestFile is compared to itself.");

            Assert.That(actual.GetFileSize(), Is.EqualTo(GetFileSize()), "Files are not of equal length.");

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
                    log.Log($"OK: '{Describe()}' is equal to '{actual.Describe()}'.");
                    return;
                }

                Assert.That(readActual, Is.EqualTo(readExpected), "Unable to read buffers of equal length.");

                for (var i = 0; i < readActual; i++)
                {
                    if (bytesExpected[i] != bytesActual[i]) Assert.Fail("File contents not equal.");
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
