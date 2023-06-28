using DistTestCore;
using Logging;

namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly K8sFactory k8SFactory = new K8sFactory();
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

            ClearAllCustomNamespaces(allTests, overviewLog);

            var testLoop = allTests.Select(t => new TestLoop(config, overviewLog, t.GetType(), t.RunTestEvery)).ToArray();

            foreach (var t in testLoop)
            {
                overviewLog.Log("Launching test-loop for " + t.Name);
                t.Begin();
                Thread.Sleep(TimeSpan.FromSeconds(15));
            }

            overviewLog.Log("All test-loops launched.");
            while (true) Thread.Sleep((2 ^ 31) - 1);
        }

        private void ClearAllCustomNamespaces(ContinuousTest[] allTests, FixtureLog log)
        {
            foreach (var test in allTests) ClearAllCustomNamespaces(test, log);
        }

        private void ClearAllCustomNamespaces(ContinuousTest test, FixtureLog log)
        {
            if (string.IsNullOrEmpty(test.CustomK8sNamespace)) return;

            log.Log($"Clearing namespace '{test.CustomK8sNamespace}'...");
            var (workflowCreator, _) = k8SFactory.CreateFacilities(config, test.CustomK8sNamespace, new DefaultTimeSet(), log);
            workflowCreator.CreateWorkflow().DeleteTestResources();
        }
    }
}
