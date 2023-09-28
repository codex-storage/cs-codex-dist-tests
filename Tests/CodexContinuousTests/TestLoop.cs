using Logging;

namespace ContinuousTests
{
    public class TestLoop
    {
        private readonly EntryPointFactory entryPointFactory;
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly ILog overviewLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;
        private readonly StartupChecker startupChecker;
        private readonly CancellationToken cancelToken;
        private readonly EventWaitHandle runFinishedHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        public TestLoop(EntryPointFactory entryPointFactory, TaskFactory taskFactory, Configuration config, ILog overviewLog, Type testType, TimeSpan runsEvery, StartupChecker startupChecker, CancellationToken cancelToken)
        {
            this.entryPointFactory = entryPointFactory;
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.testType = testType;
            this.runsEvery = runsEvery;
            this.startupChecker = startupChecker;
            this.cancelToken = cancelToken;
            Name = testType.Name;
        }

        public string Name { get; }
        public int NumberOfPasses { get; private set; }
        public int NumberOfFailures { get; private set; }

        public void Begin()
        {
            taskFactory.Run(() =>
            {
                try
                {
                    NumberOfPasses = 0;
                    NumberOfFailures = 0;
                    while (true)
                    {
                        WaitHandle.WaitAny(new[] { runFinishedHandle, cancelToken.WaitHandle });

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
            var run = new SingleTestRun(entryPointFactory, taskFactory, config, overviewLog, handle, startupChecker, cancelToken);

            runFinishedHandle.Reset();
            run.Run(runFinishedHandle, result =>
            {
                if (result) NumberOfPasses++;
                else NumberOfFailures++;
            });
        }
    }
}
