namespace AutoClient.Modes.FolderStore
{
    public class FolderWorkDispatcher
    {
        private readonly List<string> files = new List<string>();
        private readonly List<string> revisitSoon = new List<string>();
        public bool Revisiting { get; private set; } = false;

        public FolderWorkDispatcher(string folder)
        {
            var fs = Directory.GetFiles(folder);
            foreach (var f in fs)
            {
                if (!f.ToLowerInvariant().Contains(".json"))
                {
                    var info = new FileInfo(f);
                    if (info.Exists && info.Length > 1024 * 1024) // larger than 1MB
                    {
                        files.Add(f);
                    }
                }
            }
        }

        public string GetFileToCheck()
        {
            if (Revisiting)
            {
                if (!revisitSoon.Any())
                {
                    Revisiting = false;
                    return GetFileToCheck();
                }

                var file = revisitSoon.First();
                revisitSoon.RemoveAt(0);
                return file;
            }
            else
            {
                var file = files.First();
                files.RemoveAt(0);
                files.Add(file);

                if (revisitSoon.Count > 3) Revisiting = true;
                return file;
            }
        }

        public void RevisitSoon(string file)
        {
            revisitSoon.Add(file);
        }
    }
}
