namespace AutoClient.Modes.FolderStore
{
    public class FolderWorkDispatcher
    {
        private readonly string[] files = Array.Empty<string>();
        private int index = 0;

        public FolderWorkDispatcher(string folder)
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
        }

        public FileIndex GetFileToCheck()
        {
            var file = new FileIndex(files[index], index);
            index = (index + 1) % files.Length;
            return file;
        }

        public void ResetIndex()
        {
            index = 0;
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
