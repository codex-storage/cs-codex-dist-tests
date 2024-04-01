using Logging;
using Utils;

namespace FileUtils
{
    public interface IFileManager
    {
        TrackedFile CreateEmptyFile(string label = "");
        TrackedFile GenerateFile(ByteSize size, string label = "");
        void DeleteAllFiles();
        void ScopedFiles(Action action);
        T ScopedFiles<T>(Func<T> action);
    }

    public class FileManager : IFileManager
    {
        public const int ChunkSize = 1024 * 1024 * 100;
        private static NumberSource folderNumberSource = new NumberSource(0);
        private readonly Random random = new Random();
        private readonly ILog log;
        private readonly string rootFolder;
        private readonly string folder;
        private readonly List<List<TrackedFile>> fileSetStack = new List<List<TrackedFile>>();

        public FileManager(ILog log, string rootFolder)
        {
            folder = Path.Combine(rootFolder, folderNumberSource.GetNextNumber().ToString("D5"));

            this.log = log;
            this.rootFolder = rootFolder;
        }

        public TrackedFile CreateEmptyFile(string label = "")
        {
            var path = Path.Combine(folder, Guid.NewGuid().ToString() + ".bin");
            EnsureDirectory();

            var result = new TrackedFile(log, path, label);
            File.Create(result.Filename).Close();
            if (fileSetStack.Any()) fileSetStack.Last().Add(result);
            return result;
        }

        public TrackedFile GenerateFile(ByteSize size, string label)
        {
            var sw = Stopwatch.Begin(log);
            var result = GenerateRandomFile(size, label);
            sw.End($"Generated file {result.Describe()}.");
            return result;
        }

        public void DeleteAllFiles()
        {
            DeleteDirectory();
        }

        public void ScopedFiles(Action action)
        {
            PushFileSet();
            try
            {
                action();
            }
            finally
            {
                PopFileSet();
            }
        }

        public T ScopedFiles<T>(Func<T> action)
        {
            PushFileSet();
            try
            {
                return action();
            }
            finally
            {
                PopFileSet();
            }
        }

        private void PushFileSet()
        {
            fileSetStack.Add(new List<TrackedFile>());
        }

        private void PopFileSet()
        {
            if (!fileSetStack.Any()) return;
            var pop = fileSetStack.Last();
            fileSetStack.Remove(pop);

            foreach (var file in pop)
            {
                File.Delete(file.Filename);
            }

            // If the folder is now empty, delete it too.
            if (!Directory.GetFiles(folder).Any()) DeleteDirectory();
        }

        private TrackedFile GenerateRandomFile(ByteSize size, string label)
        {
            var result = CreateEmptyFile(label);
            CheckSpaceAvailable(result, size);

            GenerateFileBytes(result, size);
            return result;
        }

        private void CheckSpaceAvailable(TrackedFile testFile, ByteSize size)
        {
            var file = new FileInfo(testFile.Filename);
            var drive = new DriveInfo(file.Directory!.Root.FullName);

            var spaceAvailable = drive.TotalFreeSpace;

            if (spaceAvailable < size.SizeInBytes)
            {
                var msg = $"Not enough disk space. " +
                    $"{Formatter.FormatByteSize(size.SizeInBytes)} required. " +
                    $"{Formatter.FormatByteSize(spaceAvailable)} available.";

                log.Log(msg);
                throw new Exception(msg);
            }
        }

        private void GenerateFileBytes(TrackedFile result, ByteSize size)
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

        private void AppendRandomBytesToFile(TrackedFile result, long length)
        {
            var bytes = new byte[length];
            random.NextBytes(bytes);
            using var stream = new FileStream(result.Filename, FileMode.Append);
            stream.Write(bytes, 0, bytes.Length);
        }

        private void EnsureDirectory()
        {
            Directory.CreateDirectory(folder);
        }

        private void DeleteDirectory()
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
