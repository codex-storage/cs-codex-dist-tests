using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class FolderSaver : IFileSaverEventHandler
    {
        public const string FolderSaverFilename = "foldersaver.json";
        private readonly App app;
        private readonly LoadBalancer loadBalancer;
        private readonly JsonFile<FolderStatus> statusFile;
        private readonly FolderStatus status;
        private readonly object statusLock = new object();
        private readonly BalanceChecker balanceChecker;
        private readonly SlowModeHandler slowModeHandler;
        private int changeCounter = 0;
        private int saveFolderJsonCounter = 0;

        public FolderSaver(App app, LoadBalancer loadBalancer)
        {
            this.app = app;
            this.loadBalancer = loadBalancer;
            balanceChecker = new BalanceChecker(app);
            slowModeHandler = new SlowModeHandler(app);

            statusFile = new JsonFile<FolderStatus>(app, Path.Combine(app.Config.FolderToStore, FolderSaverFilename));
            status = statusFile.Load();
        }

        public void Run()
        {
            saveFolderJsonCounter = 0;

            var folderFiles = Directory.GetFiles(app.Config.FolderToStore);
            if (!folderFiles.Any()) throw new Exception("No files found in " + app.Config.FolderToStore);

            balanceChecker.Check();
            foreach (var folderFile in folderFiles)
            {
                if (app.Cts.IsCancellationRequested) return;
                loadBalancer.CheckErrors();

                if (!folderFile.ToLowerInvariant().EndsWith(FolderSaverFilename))
                {
                    SaveFile(folderFile);
                }

                slowModeHandler.Check();
                
                CheckAndSaveChanges();
            }

            app.Log.Log("All files processed.");
        }

        private void CheckAndSaveChanges()
        {
            if (changeCounter > 1)
            {
                changeCounter = 0;
                saveFolderJsonCounter++;
                if (saveFolderJsonCounter > 10)
                {
                    saveFolderJsonCounter = 0;
                    balanceChecker.Check();
                    SaveFolderSaverJsonFile();
                }
            }
        }

        private void SaveFile(string folderFile)
        {
            var localFilename = Path.GetFileName(folderFile);
            var entry = GetEntry(localFilename);
            ProcessFileEntry(folderFile, entry);
        }

        private FileStatus GetEntry(string localFilename)
        {
            lock (statusLock)
            {
                var entry = status.Files.SingleOrDefault(f => f.Filename == localFilename);
                if (entry != null) return entry;
                var newEntry = new FileStatus
                {
                    Filename = localFilename
                };
                status.Files.Add(newEntry);
                return newEntry;
            }
        }

        private void ProcessFileEntry(string folderFile, FileStatus entry)
        {
            var fileSaver = CreateFileSaver(folderFile, entry);
            fileSaver.Process();
        }

        private void SaveFolderSaverJsonFile()
        {
            app.Log.Log($"Saving {FolderSaverFilename}...");
            var entry = new FileStatus
            {
                Filename = FolderSaverFilename
            };
            var folderFile = Path.Combine(app.Config.FolderToStore, FolderSaverFilename);
            ApplyPadding(folderFile);
            var fileSaver = CreateFileSaver(folderFile, entry, new FolderSaveResultHandler(app, entry));
            fileSaver.Process();
        }

        private const int MinCodexStorageFilesize = 262144;
        private readonly string paddingMessage = $"Codex currently requires a minimum filesize of {MinCodexStorageFilesize} bytes for datasets used in storage contracts. " +
            $"Anything smaller, and the erasure-coding algorithms used for data durability won't function. Therefore, we apply this padding field to make sure this " +
            $"file is larger than the minimal size. The following is pseudo-random: ";

        private void ApplyPadding(string folderFile)
        {
            var info = new FileInfo(folderFile);
            var min = MinCodexStorageFilesize * 2;
            if (info.Length < min)
            {
                var required = Math.Max(1024, min - info.Length);
                lock (statusLock)
                {
                    status.Padding = paddingMessage + RandomUtils.GenerateRandomString(required);
                    statusFile.Save(status);
                }
            }
        }

        private FileSaver CreateFileSaver(string folderFile, FileStatus entry)
        {
            return CreateFileSaver(folderFile, entry, slowModeHandler);
        }

        private FileSaver CreateFileSaver(string folderFile, FileStatus entry, IFileSaverResultHandler resultHandler)
        {
            var fixedLength = entry.Filename.PadRight(35);
            var prefix = $"[{fixedLength}] ";
            return new FileSaver(new LogPrefixer(app.Log, prefix), loadBalancer, status.Stats, folderFile, entry, this, resultHandler);
        }

        public void SaveChanges()
        {
            lock (statusLock)
            {
                statusFile.Save(status);
            }
            changeCounter++;
        }
    }

    public class FolderSaveResultHandler : IFileSaverResultHandler
    {
        private readonly App app;
        private readonly FileStatus entry;

        public FolderSaveResultHandler(App app, FileStatus entry)
        {
            this.app = app;
            this.entry = entry;
        }

        public void OnFailure()
        {
            app.Log.Error($"Failed to store {FolderSaver.FolderSaverFilename} :|");
        }

        public void OnSuccess()
        {
            if (!string.IsNullOrEmpty(entry.EncodedCid))
            {
                var cidsFile = Path.Combine(app.Config.DataPath, "cids.log");
                File.AppendAllLines(cidsFile, [entry.EncodedCid]);
                app.Log.Log($"!!! {FolderSaver.FolderSaverFilename} saved to CID '{entry.EncodedCid}' !!!");
            }
            else
            {

                app.Log.Error($"Foldersaver entry didn't have encoded CID somehow :|");
            }
        }
    }
}
