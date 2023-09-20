using DistTestCore.Logs;
using Logging;

namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly EntryPointFactory entryPointFactory = new EntryPointFactory();
        private readonly ConfigLoader configLoader = new ConfigLoader();
        private readonly TestFactory testFactory = new TestFactory();
        private readonly Configuration config;
        private readonly CancellationToken cancelToken;

        public ContinuousTestRunner(string[] args, CancellationToken cancelToken)
        {
            config = configLoader.Load(args);
            this.cancelToken = cancelToken;
        }

        public void Run()
        {
            var overviewLog = new FixtureLog(new LogConfig(config.LogPath, false), DateTime.UtcNow, "Overview");

            var entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, overviewLog);
            entryPoint.Announce();

            var startupChecker = new StartupChecker(entryPoint, config, cancelToken);
            startupChecker.Check();

            var taskFactory = new TaskFactory();
            overviewLog.Log("Continuous tests starting...");
            var allTests = testFactory.CreateTests();

            ClearAllCustomNamespaces(allTests, overviewLog);

            var testLoops = allTests.Select(t => new TestLoop(entryPoint, taskFactory, config, overviewLog, t.GetType(), t.RunTestEvery, startupChecker, cancelToken)).ToArray();

            foreach (var testLoop in testLoops)
            {
                if (cancelToken.IsCancellationRequested) break;

                overviewLog.Log("Launching test-loop for " + testLoop.Name);
                testLoop.Begin();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            overviewLog.Log("Finished launching test-loops.");
            cancelToken.WaitHandle.WaitOne();
            overviewLog.Log("Cancelling all test-loops...");
            taskFactory.WaitAll();
            overviewLog.Log("All tasks cancelled.");
        }

        private void ClearAllCustomNamespaces(ContinuousTest[] allTests, FixtureLog log)
        {
            foreach (var test in allTests) ClearAllCustomNamespaces(test, log);
        }

        private void ClearAllCustomNamespaces(ContinuousTest test, FixtureLog log)
        {
            if (string.IsNullOrEmpty(test.CustomK8sNamespace)) return;

            log.Log($"Clearing namespace '{test.CustomK8sNamespace}'...");

            var entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, test.CustomK8sNamespace, log);
            entryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(test.CustomK8sNamespace);
        }
    }
}
