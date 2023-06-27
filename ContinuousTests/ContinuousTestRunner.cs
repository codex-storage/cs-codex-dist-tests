using Logging;

namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly ConfigLoader configLoader = new ConfigLoader();
        private readonly TestFactory testFactory = new TestFactory();
        private readonly Configuration config;
        private readonly StartupChecker startupChecker;

        public ContinuousTestRunner(string[] args)
        {
            config = configLoader.Load(args);
            startupChecker = new StartupChecker(config);
        }

        public void Run()
        {
            startupChecker.Check();

            var overviewLog = new FixtureLog(new LogConfig(config.LogPath, false), "Overview");
            overviewLog.Log("Continuous tests starting...");
            var allTests = testFactory.CreateTests();
            var testLoop = allTests.Select(t => new TestLoop(config, overviewLog, t.GetType(), t.RunTestEvery)).ToArray();

            foreach (var t in testLoop)
            {
                overviewLog.Log("Launching test-loop for " + t.Name);
                t.Begin();
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }

            overviewLog.Log("All test-loops launched.");
            while (true) Thread.Sleep((2 ^ 31) - 1);
        }
    }
}
