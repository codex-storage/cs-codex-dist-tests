using DistTestCore.Logs;
using Logging;
using Utils;

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
            var logConfig = new LogConfig(config.LogPath, false);
            var startTime = DateTime.UtcNow;

            var overviewLog = new LogSplitter(
                new FixtureLog(logConfig, startTime, "Overview"),
                new ConsoleLog()
            );
            var statusLog = new StatusLog(logConfig, startTime, "ContinuousTestRun");

            overviewLog.Log("Initializing...");

            var entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, overviewLog);
            entryPoint.Announce();

            overviewLog.Log("Initialized. Performing startup checks...");

            var startupChecker = new StartupChecker(entryPoint, config, cancelToken);
            startupChecker.Check();

            var taskFactory = new TaskFactory();
            overviewLog.Log("Startup checks passed. Continuous tests starting...");
            overviewLog.Log("");
            var allTests = testFactory.CreateTests();

            ClearAllCustomNamespaces(allTests, overviewLog);

            var filteredTests = FilterTests(allTests, overviewLog);
            if (!filteredTests.Any())
            {
                overviewLog.Log("No tests selected.");
                return;
            }
            var testLoops = filteredTests.Select(t => new TestLoop(entryPointFactory, taskFactory, config, overviewLog, t.GetType(), t.RunTestEvery, startupChecker, cancelToken)).ToArray();

            foreach (var testLoop in testLoops)
            {
                if (cancelToken.IsCancellationRequested) break;

                overviewLog.Log("Launching test-loop for " + testLoop.Name);
                testLoop.Begin();
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            overviewLog.Log("Finished launching test-loops.");
            WaitUntilFinished(overviewLog, statusLog, startTime, testLoops);
            overviewLog.Log("Cancelling all test-loops...");
            taskFactory.WaitAll();
            overviewLog.Log("All tasks cancelled.");

            PerformCleanup(overviewLog);
        }

        private ContinuousTest[] FilterTests(ContinuousTest[] allTests, ILog log)
        {
            log.Log($"Available tests: {string.Join(", ", allTests.Select(r => r.Name))}");

            var result = allTests.ToArray();
            var filters = config.Filter.Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (filters.Any())
            {
                log.Log($"Applying filters: {string.Join(", ", filters)}");
                result = allTests.Where(t => filters.Any(f => t.Name.Contains(f))).ToArray();
            }

            log.Log($"Selected for running: {string.Join(", ", result.Select(r => r.Name))}");
            return result;
        }

        private void WaitUntilFinished(LogSplitter overviewLog, StatusLog statusLog, DateTime startTime, TestLoop[] testLoops)
        {
            var testDuration = Time.FormatDuration(DateTime.UtcNow - startTime);
            var testData = FormatTestRuns(testLoops);

            if (config.TargetDurationSeconds > 0)
            {
                var targetDuration = TimeSpan.FromSeconds(config.TargetDurationSeconds);
                cancelToken.WaitHandle.WaitOne(targetDuration);
                overviewLog.Log($"Congratulations! The targer duration has been reached! ({Time.FormatDuration(targetDuration)})");
                statusLog.ConcludeTest("Passed", testDuration, testData);
            }
            else
            {
                cancelToken.WaitHandle.WaitOne();
                statusLog.ConcludeTest("Failed", testDuration, testData);
            }
        }

        private Dictionary<string, string> FormatTestRuns(TestLoop[] testLoops)
        {
            var result = new Dictionary<string, string>();
            foreach (var testLoop in testLoops)
            {
                result.Add($"ctest-{testLoop.Name}", $"passes: {testLoop.NumberOfPasses} - failures: {testLoop.NumberOfFailures}");
            }
            return result;
        }

        private void ClearAllCustomNamespaces(ContinuousTest[] allTests, ILog log)
        {
            foreach (var test in allTests) ClearAllCustomNamespaces(test, log);
        }

        private void ClearAllCustomNamespaces(ContinuousTest test, ILog log)
        {
            if (string.IsNullOrEmpty(test.CustomK8sNamespace)) return;

            log.Log($"Clearing namespace '{test.CustomK8sNamespace}'...");

            var entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, test.CustomK8sNamespace, log);
            entryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(test.CustomK8sNamespace);
        }

        private void PerformCleanup(ILog log)
        {
            if (!config.Cleanup) return;
            log.Log("Cleaning up test namespace...");

            var entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, log);
            entryPoint.Decommission(deleteKubernetesResources: true, deleteTrackedFiles: true);
            log.Log("Cleanup finished.");
        }
    }
}
