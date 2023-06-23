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
            var tests = testFinder.CreateTests().ToList();
            var handles = tests.Select(t => new TestHandle(t)).ToArray();

            var result = ContinuousTestResult.Passed;
            while (tests.Any())
            {
                var test = tests.PickOneRandom();
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
