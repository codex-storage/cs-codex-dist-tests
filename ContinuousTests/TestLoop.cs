using Logging;

namespace ContinuousTests
{
    public class TestLoop
    {
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;
        private readonly CancellationToken cancelToken;

        public TestLoop(TaskFactory taskFactory, Configuration config, BaseLog overviewLog, Type testType, TimeSpan runsEvery, CancellationToken cancelToken)
        {
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.testType = testType;
            this.runsEvery = runsEvery;
            this.cancelToken = cancelToken;
            Name = testType.Name;
        }

        public string Name { get; }

        public void Begin()
        {
            taskFactory.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        cancelToken.ThrowIfCancellationRequested();
                        StartTest();

                        cancelToken.WaitHandle.WaitOne(runsEvery);
                    }
                }
                catch (OperationCanceledException)
                {
                    overviewLog.Log("Test-loop " + testType.Name + " is cancelled.");
                }
                catch (Exception ex)
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
            var run = new SingleTestRun(taskFactory, config, overviewLog, handle, cancelToken);
            run.Run();
        }
    }
}
