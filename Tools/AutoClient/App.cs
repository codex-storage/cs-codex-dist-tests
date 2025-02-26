using AutoClient.Modes.FolderStore;
using CodexClient;
using Logging;
using WebUtils;

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

            var httpFactory = new HttpFactory(Log, new AutoClientWebTimeSet());

            CodexNodeFactory = new CodexNodeFactory(log: Log, httpFactory: httpFactory, dataDir: Config.DataPath);
        }

        public Configuration Config { get; }
        public ILog Log { get; }
        public IFileGenerator Generator { get; }
        public CancellationTokenSource Cts { get; } = new CancellationTokenSource();
        public Performance Performance { get; }
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

    public class AutoClientWebTimeSet : IWebCallTimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(30.0);
        }

        public TimeSpan HttpRetryTimeout()
        {
            return HttpCallTimeout() * 2.2;
        }

        /// <summary>
        /// After a failed HTTP call, wait this long before trying again.
        /// </summary>
        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromMinutes(1.0);
        }
    }
}
