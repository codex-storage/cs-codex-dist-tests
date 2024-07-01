using Logging;
using Utils;

namespace FileUtils
{
    public interface IFileManager
    {
        TrackedFile CreateEmptyFile(string label = "");
        TrackedFile GenerateFile(ByteSize size, string label = "");
        TrackedFile GenerateFile(Action<IGenerateOption> options, string label = "");
        void DeleteAllFiles();
        void ScopedFiles(Action action);
        T ScopedFiles<T>(Func<T> action);
    }

    public interface IGenerateOption
    {
        IGenerateOption Random(ByteSize size);
        IGenerateOption StringRepeat(string str, ByteSize size);
        IGenerateOption StringRepeat(string str, int times);
        IGenerateOption ByteRepeat(byte[] bytes, ByteSize size);
        IGenerateOption ByteRepeat(byte[] bytes, int times);
    }

    public class FileManager : IFileManager
    {
        private static readonly NumberSource folderNumberSource = new NumberSource(0);
        private readonly ILog log;
        private readonly string folder;
        private readonly List<List<TrackedFile>> fileSetStack = new List<List<TrackedFile>>();

        public const int ChunkSize = 1024 * 1024 * 100;

        public FileManager(ILog log, string rootFolder)
        {
            folder = Path.Combine(rootFolder, folderNumberSource.GetNextNumber().ToString("D5"));

            this.log = log;
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

        public TrackedFile GenerateFile(ByteSize size, string label = "")
        {
            return GenerateFile(o => o.Random(size), label);
        }

        public TrackedFile GenerateFile(Action<IGenerateOption> options, string label = "")
        {
            var sw = Stopwatch.Begin(log);
            var result = RunGenerators(options, label);
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

        private TrackedFile RunGenerators(Action<IGenerateOption> options, string label)
        {
            var result = CreateEmptyFile(label);
            var generators = GetGenerators(options);
            CheckSpaceAvailable(result, generators.GetRequiredSpace());

            using var stream = new FileStream(result.Filename, FileMode.Append);
            generators.Run(stream);
            return result;
        }

        private GeneratorCollection GetGenerators(Action<IGenerateOption> options)
        {
            var result = new GeneratorCollection();
            options(result);
            return result;
        }

        private void CheckSpaceAvailable(TrackedFile testFile, long requiredSize)
        {
            var file = new FileInfo(testFile.Filename);
            var drive = new DriveInfo(file.Directory!.Root.FullName);

            var spaceAvailable = drive.TotalFreeSpace;

            if (spaceAvailable < requiredSize)
            {
                var msg = $"Not enough disk space. " +
                    $"{Formatter.FormatByteSize(requiredSize)} required. " +
                    $"{Formatter.FormatByteSize(spaceAvailable)} available.";

                log.Log(msg);
                throw new Exception(msg);
            }
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
