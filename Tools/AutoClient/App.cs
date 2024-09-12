using Logging;

namespace AutoClient
{
    public class App
    {
        public App(Configuration config)
        {
            Config = config;

            Log = new LogSplitter(
                new FileLog(Path.Combine(config.LogPath, "autoclient")),
                new ConsoleLog()
            );

            Generator = CreateGenerator();
            CidRepo = new CidRepo(config);
        }

        public Configuration Config { get; }
        public ILog Log { get; }
        public IFileGenerator Generator { get; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
        public CidRepo CidRepo { get; }
        public Performance Performance { get; } = new Performance();

        private IFileGenerator CreateGenerator()
        {
            if (Config.FileSizeMb > 0)
            {
                return new RandomFileGenerator(Config, Log);
            }
            return new ImageGenerator(Log);
        }
    }
}
