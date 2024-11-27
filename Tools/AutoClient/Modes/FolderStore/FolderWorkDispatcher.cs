using Logging;

namespace AutoClient.Modes.FolderStore
{
    public class FolderWorkDispatcher
    {
        private readonly string[] files = Array.Empty<string>();
        private readonly ILog log;
        private int index = 0;
        private int busyCount = 0;

        public FolderWorkDispatcher(ILog log, string folder)
        {
            var fs = Directory.GetFiles(folder);
            var result = new List<string>();
            foreach (var f in fs)
            {
                if (!f.ToLowerInvariant().Contains(".json"))
                {
                    var info = new FileInfo(f);
                    if (info.Exists && info.Length > 1024 * 1024) // larger than 1MB
                    {
                        result.Add(f);
                    }
                }
            }
            files = result.ToArray();
            this.log = log;
        }

        public FileIndex GetFileToCheck()
        {
            if (busyCount > 1)
            {
                log.Log("");
                log.Log("Max number of busy workers reached. Waiting until contracts are started before creating any more.");
                log.Log("");
                ResetIndex();
            }

            var file = new FileIndex(files[index], index);
            index = (index + 1) % files.Length;
            return file;
        }

        public void ResetIndex()
        {
            index = 0;
            busyCount = 0;
        }

        public void WorkerIsBusy()
        {
            busyCount++;
        }
    }

    public class FileIndex
    {
        public FileIndex(string file, int index)
        {
            File = file;
            Index = index;
        }

        public string File { get; }
        public int Index { get; }
    }
}
