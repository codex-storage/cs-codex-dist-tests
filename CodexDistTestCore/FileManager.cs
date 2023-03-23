using NUnit.Framework;

namespace CodexDistTestCore
{
    public interface IFileManager
    {
        TestFile CreateEmptyTestFile();
        TestFile GenerateTestFile(ByteSize size);
        void DeleteAllTestFiles();
    }

    public class FileManager : IFileManager
    {
        public const int ChunkSize = 1024 * 1024;
        private const string Folder = "TestDataFiles";
        private readonly Random random = new Random();
        private readonly List<TestFile> activeFiles = new List<TestFile>();
        private readonly TestLog log;

        public FileManager(TestLog log)
        {
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
            this.log = log;
        }

        public TestFile CreateEmptyTestFile()
        {
            var result = new TestFile(Path.Combine(Folder, Guid.NewGuid().ToString() + "_test.bin"));
            File.Create(result.Filename).Close();
            activeFiles.Add(result);
            return result;
        }

        public TestFile GenerateTestFile(ByteSize size)
        {
            var result = CreateEmptyTestFile();
            GenerateFileBytes(result, size);
            log.Log($"Generated {size.SizeInBytes} bytes of content for file '{result.Filename}'.");
            return result;
        }

        public void DeleteAllTestFiles()
        {
            foreach (var file in activeFiles) File.Delete(file.Filename);
            activeFiles.Clear();
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
    }

    public class TestFile
    {
        public TestFile(string filename)
        {
            Filename = filename;
        }

        public string Filename { get; }

        public long GetFileSize()
        {
            var info = new FileInfo(Filename);
            return info.Length;
        }

        public void AssertIsEqual(TestFile? other)
        {
            if (other == null) Assert.Fail("TestFile is null.");
            if (other == this || other!.Filename == Filename) Assert.Fail("TestFile is compared to itself.");

            using var stream1 = new FileStream(Filename, FileMode.Open, FileAccess.Read);
            using var stream2 = new FileStream(other.Filename, FileMode.Open, FileAccess.Read);

            var bytes1 = new byte[FileManager.ChunkSize];
            var bytes2 = new byte[FileManager.ChunkSize];

            var read1 = 0;
            var read2 = 0;

            while (true)
            {
                read1 = stream1.Read(bytes1, 0, FileManager.ChunkSize);
                read2 = stream2.Read(bytes2, 0, FileManager.ChunkSize);

                if (read1 == 0 && read2 == 0) return;
                Assert.That(read1, Is.EqualTo(read2), "Files are not of equal length.");
                CollectionAssert.AreEqual(bytes1, bytes2, "Files are not binary-equal.");
            }
        }
    }
}
