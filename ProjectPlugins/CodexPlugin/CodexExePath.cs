namespace CodexPlugin
{
    public class CodexExePath
    {
        private readonly string path = Path.Combine(
            "d:",
            "Dev",
            "nim-codex",
            "build",
            "codex.exe"
        );

        public string Get()
        {
            return path;
        }
    }
}
