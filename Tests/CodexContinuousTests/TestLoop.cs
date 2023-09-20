using Core;
using Logging;

namespace ContinuousTests
{
    public class TestLoop
    {
        private readonly EntryPoint entryPoint;
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly Type testType;
        private readonly TimeSpan runsEvery;
        private readonly StartupChecker startupChecker;
        private readonly CancellationToken cancelToken;
        private readonly EventWaitHandle runFinishedHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        public TestLoop(Core.EntryPoint entryPoint, TaskFactory taskFactory, Configuration config, BaseLog overviewLog, Type testType, TimeSpan runsEvery, StartupChecker startupChecker, CancellationToken cancelToken)
        {
            this.entryPoint = entryPoint;
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

        public void Begin()
        {
            taskFactory.Run(() =>
            {
                try
                {
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
            var run = new SingleTestRun(entryPoint, taskFactory, config, overviewLog, handle, startupChecker, cancelToken);

            runFinishedHandle.Reset();
            run.Run(runFinishedHandle);
        }
    }
}
