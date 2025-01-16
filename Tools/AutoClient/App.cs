using AutoClient.Modes.FolderStore;
using CodexClient;
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

            CodexNodeFactory = new CodexNodeFactory(log: Log, dataDir: Config.DataPath);
        }

        public Configuration Config { get; }
        public ILog Log { get; }
        public IFileGenerator Generator { get; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
        public Performance Performance { get; }
        public FolderWorkDispatcher FolderWorkDispatcher { get; }
        public CodexNodeFactory CodexNodeFactory { get; }

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
