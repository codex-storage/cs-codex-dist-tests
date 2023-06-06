using Logging;
using NUnit.Framework;
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
        public const int ChunkSize = 1024 * 1024;
        private static NumberSource folderNumberSource = new NumberSource(0);
        private readonly Random random = new Random();
        private readonly TestLog log;
        private readonly string folder;
        private readonly List<List<TestFile>> fileSetStack = new List<List<TestFile>>();

        public FileManager(TestLog log, Configuration configuration)
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
            var result = CreateEmptyTestFile(label);
            GenerateFileBytes(result, size);
            log.Log($"Generated {size} of content for file '{result.Describe()}'.");
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

        private void GenerateFileBytes(TestFile result, ByteSize size)
        {
            long bytesLeft = size.SizeInBytes;
            while (bytesLeft > 0)
            {
                var length = Math.Min(bytesLeft, ChunkSize);
                AppendRandomBytesToFile(result, length);
                bytesLeft -= length;
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
        private readonly TestLog log;

        public TestFile(TestLog log, string filename, string label)
        {
            this.log = log;
            Filename = filename;
            Label = label;
        }

        public string Filename { get; }
        public string Label { get; }

        public long GetFileSize()
        {
            var info = new FileInfo(Filename);
            return info.Length;
        }

        public void AssertIsEqual(TestFile? actual)
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
                CollectionAssert.AreEqual(bytesExpected, bytesActual, "Files are not binary-equal.");
            }
        }

        public string Describe()
        {
            if (!string.IsNullOrEmpty(Label)) return Label;
            return Filename;
        }
    }
}
