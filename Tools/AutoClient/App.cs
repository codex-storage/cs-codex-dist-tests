using AutoClient.Modes.FolderStore;
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
            Performance = new Performance(new LogSplitter(
                new FileLog(Path.Combine(config.LogPath, "performance")),
                new ConsoleLog()
            ));

            if (!string.IsNullOrEmpty(config.FolderToStore))
            {
                FolderWorkDispatcher = new FolderWorkDispatcher(Log, config.FolderToStore);
            }
            else
            {
                FolderWorkDispatcher = null!;
            }
        }

        public Configuration Config { get; }
        public ILog Log { get; }
        public IFileGenerator Generator { get; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
        public CidRepo CidRepo { get; }
        public Performance Performance { get; }
        public FolderWorkDispatcher FolderWorkDispatcher { get; }

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
