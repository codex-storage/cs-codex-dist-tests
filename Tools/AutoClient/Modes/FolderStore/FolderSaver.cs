using CodexClient;
using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class FolderSaver
    {
        private const string FolderSaverFilename = "foldersaver.json";
        private readonly App app;
        private readonly CodexWrapper instance;
        private readonly JsonFile<FolderStatus> statusFile;
        private readonly FolderStatus status;
        private int failureCount = 0;

        public FolderSaver(App app, CodexWrapper instance)
        {
            this.app = app;
            this.instance = instance;

            statusFile = new JsonFile<FolderStatus>(app, Path.Combine(app.Config.FolderToStore, FolderSaverFilename));
            status = statusFile.Load();
        }

        public void Run(CancellationTokenSource cts)
        {
            var folderFiles = Directory.GetFiles(app.Config.FolderToStore);
            if (!folderFiles.Any()) throw new Exception("No files found in " + app.Config.FolderToStore);

            var counter = 0;
            foreach (var folderFile in folderFiles)
            {
                if (cts.IsCancellationRequested) return;

                if (!folderFile.ToLowerInvariant().EndsWith(FolderSaverFilename))
                {
                    SaveFile(folderFile);
                    counter++;
                }

                if (failureCount > 9)
                {
                    app.Log.Error("Failure count reached threshold. Stopping...");
                    cts.Cancel();
                    return;
                }

                if (counter > 5)
                {
                    counter = 0;
                    SaveFolderSaverJsonFile();
                }

                Thread.Sleep(2000);
            }
        }

        private void SaveFile(string folderFile)
        {
            var localFilename = Path.GetFileName(folderFile);
            var entry = status.Files.SingleOrDefault(f => f.Filename == localFilename);
            if (entry == null)
            {
                entry = new FileStatus();
                status.Files.Add(entry);
            }
            ProcessFileEntry(folderFile, entry);
            statusFile.Save(status);
        }

        private void ProcessFileEntry(string folderFile, FileStatus entry)
        {
            var fileSaver = CreateFileSaver(folderFile, entry);
            fileSaver.Process();
            if (fileSaver.HasFailed) failureCount++;
        }

        private void SaveFolderSaverJsonFile()
        {
            var entry = new FileStatus
            {
                Filename = FolderSaverFilename
            };
            var folderFile = Path.Combine(app.Config.FolderToStore, FolderSaverFilename);
            var fileSaver = CreateFileSaver(folderFile, entry);
            fileSaver.Process();
            if (fileSaver.HasFailed) failureCount++;
        }

        private FileSaver CreateFileSaver(string folderFile, FileStatus entry)
        {
            var fixedLength = entry.Filename.PadRight(35);
            var prefix = $"[{fixedLength}] ";
            return new FileSaver(new LogPrefixer(app.Log, prefix), instance, folderFile, entry);
        }
    }
}
