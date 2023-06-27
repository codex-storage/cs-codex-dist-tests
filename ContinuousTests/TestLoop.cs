using Logging;

namespace ContinuousTests
{
    public class TestLoop
    {
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;

        public TestLoop(Configuration config, BaseLog overviewLog, Type testType, TimeSpan runsEvery)
        {
            this.config = config;
            this.overviewLog = overviewLog;
            this.testType = testType;
            this.runsEvery = runsEvery;

            Name = testType.Name;
        }

        public string Name { get; }

        public void Begin()
        {
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        StartTest();
                        Thread.Sleep(runsEvery);
                    }
                }
                catch(Exception ex)
                {
                    overviewLog.Error("Test infra failure: TestLoop failed with " + ex);
                    Environment.Exit(-1);
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
