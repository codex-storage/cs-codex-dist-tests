using CodexContractsPlugin;

namespace CodexPlugin
{
    public class CodexDockerImage : ICodexDockerImageProvider
    {
        private const string DefaultDockerImage = "codexstorage/nim-codex:latest-dist-tests";

        public static string Override { get; set; } = string.Empty;

        public string GetCodexDockerImage()
        {
            var image = Environment.GetEnvironmentVariable("CODEXDOCKERIMAGE");
            if (!string.IsNullOrEmpty(image)) return image;
            if (!string.IsNullOrEmpty(Override)) return Override;
            return DefaultDockerImage;
        }
    }
}
