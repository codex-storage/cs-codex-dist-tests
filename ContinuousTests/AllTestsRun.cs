using DistTestCore;
using DistTestCore.Codex;
using Logging;
using Utils;

namespace ContinuousTests
{
    public class AllTestsRun
    {
        private readonly Configuration config;
        private readonly FixtureLog log;
        private readonly TestFactory testFinder;

        public AllTestsRun(Configuration config, FixtureLog log, TestFactory testFinder)
        {
            this.config = config;
            this.log = log;
            this.testFinder = testFinder;
        }

        public ContinuousTestResult RunAll()
        {
            var remainingTests = testFinder.CreateTests().ToList();
            var result = ContinuousTestResult.Passed;
            while (remainingTests.Any())
            {
                var test = remainingTests.PickOneRandom();
                var testLog = log.CreateTestLog(test.Name);
                var singleTestRun = new SingleTestRun(config, test, testLog);

                log.Log($"Start '{test.Name}'");
                try
                {
                    singleTestRun.Run();
                    log.Log($"'{test.Name}' = Passed");
                }
                catch
                {
                    log.Log($"'{test.Name}' = Failed");
                    testLog.MarkAsFailed();
                    result = ContinuousTestResult.Failed;
                }
                
                Thread.Sleep(config.SleepSecondsPerSingleTest * 1000);
            }
            return result;
        }
    }

    public class SingleTestRun
    {
        private readonly CodexNodeFactory codexNodeFactory = new CodexNodeFactory();
        private readonly Configuration config;
        private readonly ContinuousTest test;
        private readonly CodexNode[] nodes;
        private readonly FileManager fileManager;

        public SingleTestRun(Configuration config, ContinuousTest test, BaseLog testLog)
        {
            this.config = config;
            this.test = test;

            nodes = CreateRandomNodes(test.RequiredNumberOfNodes, testLog);
            fileManager = new FileManager(testLog, new DistTestCore.Configuration());

            test.Initialize(nodes, testLog, fileManager);
        }

        public void Run()
        {
            test.Run();
        }

        public void TearDown()
        {
            test.Initialize(null!, null!, null!);
            fileManager.DeleteAllTestFiles();
        }

        private CodexNode[] CreateRandomNodes(int number, BaseLog testLog)
        {
            var urls = SelectRandomUrls(number);
            return codexNodeFactory.Create(urls, testLog, test.TimeSet);
        }

        private string[] SelectRandomUrls(int number)
        {
            var urls = config.CodexUrls.ToList();
            var result = new string[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = urls.PickOneRandom();
            }
            return result;
        }
    }
}
