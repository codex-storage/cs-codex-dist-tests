namespace CodexPlugin
{
    public class CodexExePath
    {
        private readonly string[] paths = [
            Path.Combine("d:", "Dev", "nim-codex", "build", "codex.exe"),
            Path.Combine("c:", "Projects", "nim-codex", "build", "codex.exe")
        ];

        private string selectedPath = string.Empty;

        public CodexExePath()
        {
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    selectedPath = p;
                    return;
                }
            }
        }

        public string Get()
        {
            return selectedPath;
        }
    }
}
