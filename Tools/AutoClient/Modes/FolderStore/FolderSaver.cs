﻿using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class FolderSaver
    {
        private const string FolderSaverFilename = "foldersaver.json";
        private readonly App app;
        private readonly CodexWrapper instance;
        private readonly JsonFile<FolderStatus> statusFile;
        private readonly FolderStatus status;
        private int changeCounter = 0;
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

            changeCounter = 0;
            foreach (var folderFile in folderFiles)
            {
                if (cts.IsCancellationRequested) return;

                if (!folderFile.ToLowerInvariant().EndsWith(FolderSaverFilename))
                {
                    SaveFile(folderFile);
                }

                if (failureCount > 9)
                {
                    app.Log.Error("Failure count reached threshold. Stopping...");
                    cts.Cancel();
                    return;
                }

                if (changeCounter > 10)
                {
                    changeCounter = 0;
                    SaveFolderSaverJsonFile();
                }

                statusFile.Save(status);
                Thread.Sleep(100);
            }
        }

        private void SaveFile(string folderFile)
        {
            var localFilename = Path.GetFileName(folderFile);
            var entry = status.Files.SingleOrDefault(f => f.Filename == localFilename);
            if (entry == null)
            {
                entry = new FileStatus
                {
                    Filename = localFilename
                };
                status.Files.Add(entry);
            }
            ProcessFileEntry(folderFile, entry);
        }

        private void ProcessFileEntry(string folderFile, FileStatus entry)
        {
            var fileSaver = CreateFileSaver(folderFile, entry);
            fileSaver.Process();
            if (fileSaver.HasFailed) failureCount++;
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
            var fileSaver = CreateFileSaver(folderFile, entry);
            fileSaver.Process();
            if (fileSaver.HasFailed) failureCount++;

            app.Log.Log($"!!! {FolderSaverFilename} saved to CID '{entry.EncodedCid}' !!!");
        }

        private const int MinCodexStorageFilesize = 262144;
        private readonly Random random = new Random();
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
                status.Padding = paddingMessage + GenerateRandomString(required);
                statusFile.Save(status);
            }
        }

        private string GenerateRandomString(long required)
        {
            var result = "";
            while (result.Length < required)
            {
                var bytes = new byte[1024];
                random.NextBytes(bytes);
                result += string.Join("", bytes.Select(b => b.ToString()));
            }

            return result;
        }

        private FileSaver CreateFileSaver(string folderFile, FileStatus entry)
        {
            var fixedLength = entry.Filename.PadRight(35);
            var prefix = $"[{fixedLength}] ";
            return new FileSaver(new LogPrefixer(app.Log, prefix), instance, status.Stats, folderFile, entry, saveChanges: () =>
            {
                statusFile.Save(status);
                changeCounter++;
            });
        }
    }
}
