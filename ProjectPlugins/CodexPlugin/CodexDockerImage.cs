namespace CodexPlugin
{
    public class CodexDockerImage
    {
        private const string DefaultDockerImage = "codexstorage/nim-codex:sha-f35058d-dist-tests";

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
