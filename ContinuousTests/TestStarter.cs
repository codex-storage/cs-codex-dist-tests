using Logging;

namespace ContinuousTests
{
    public class TestStarter
    {
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;

        public TestStarter(Configuration config, BaseLog overviewLog, Type testType, TimeSpan runsEvery)
        {
            this.config = config;
            this.overviewLog = overviewLog;
            this.testType = testType;
            this.runsEvery = runsEvery;
        }

        public void Begin()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    StartTest();
                    Thread.Sleep(runsEvery);
                }
            });
        }

        private void StartTest()
        {
            var test = (ContinuousTest)Activator.CreateInstance(testType)!;
            var handle = new TestHandle(test);
            var run = new SingleTestRun(config, overviewLog, handle);
            run.Run();
        }
    }
}
