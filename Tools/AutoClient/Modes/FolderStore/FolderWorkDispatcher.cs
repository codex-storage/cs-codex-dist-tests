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

        public string GetFileToCheck()
        {
            var file = files[index];
            index = (index + 1) % files.Length;
            return file;
        }

        public void ResetIndex()
        {
            index = 0;
        }
    }
}
