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
                    if (!config.KeepPassedTestLogs) testLog.Delete();
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
}
