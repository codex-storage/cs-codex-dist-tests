namespace ContinuousTests
{
    public class TestStarter
    {
        private readonly Configuration config;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;

        public TestStarter(Configuration config, Type testType, TimeSpan runsEvery)
        {
            this.config = config;
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
            var run = new SingleTestRun(config, handle);
            run.Run();
        }
    }
}
