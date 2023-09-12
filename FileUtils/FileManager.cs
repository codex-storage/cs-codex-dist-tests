using Logging;
using NUnit.Framework;
using Utils;

namespace FileUtils
{
    public interface IFileManager
    {
        TestFile CreateEmptyTestFile(string label = "");
        TestFile GenerateTestFile(ByteSize size, string label = "");
        void DeleteAllTestFiles();
        void ScopedFiles(Action action);
        T ScopedFiles<T>(Func<T> action);
    }

    public class FileManager : IFileManager
    {
        public const int ChunkSize = 1024 * 1024 * 100;
        private static NumberSource folderNumberSource = new NumberSource(0);
        private readonly Random random = new Random();
        private readonly ILog log;
        private readonly string folder;
        private readonly List<List<TestFile>> fileSetStack = new List<List<TestFile>>();

        public FileManager(ILog log, string rootFolder)
        {
            folder = Path.Combine(rootFolder, folderNumberSource.GetNextNumber().ToString("D5"));

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

        public void ScopedFiles(Action action)
        {
            PushFileSet();
            action();
            PopFileSet();
        }

        public T ScopedFiles<T>(Func<T> action)
        {
            PushFileSet();
            var result = action();
            PopFileSet();
            return result;
        }

        private void PushFileSet()
        {
            fileSetStack.Add(new List<TestFile>());
        }

        private void PopFileSet()
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
}
