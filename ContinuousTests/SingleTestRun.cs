using DistTestCore.Codex;
using DistTestCore;
using Logging;
using Utils;
using KubernetesWorkflow;
using NUnit.Framework.Internal;
using System.Reflection;
using static Program;

namespace ContinuousTests
{
    public class SingleTestRun
    {
        private readonly CodexAccessFactory codexNodeFactory = new CodexAccessFactory();
        private readonly List<Exception> exceptions = new List<Exception>();
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly TestHandle handle;
        private readonly CancellationToken cancelToken;
        private readonly CodexAccess[] nodes;
        private readonly FileManager fileManager;
        private readonly FixtureLog fixtureLog;
        private readonly string testName;
        private readonly string dataFolder;

        public SingleTestRun(TaskFactory taskFactory, Configuration config, BaseLog overviewLog, TestHandle handle, CancellationToken cancelToken)
        {
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.handle = handle;
            this.cancelToken = cancelToken;
            testName = handle.Test.GetType().Name;
            fixtureLog = new FixtureLog(new LogConfig(config.LogPath, true), DateTime.UtcNow, testName);

            nodes = CreateRandomNodes(handle.Test.RequiredNumberOfNodes);
            dataFolder = config.DataPath + "-" + Guid.NewGuid();
            fileManager = new FileManager(fixtureLog, CreateFileManagerConfiguration());
        }

        public void Run(EventWaitHandle runFinishedHandle)
        {
            taskFactory.Run(() =>
            {
                try
                {
                    RunTest();
                    fileManager.DeleteAllTestFiles();
                    Directory.Delete(dataFolder, true);
                    runFinishedHandle.Set();
                }
                catch (Exception ex)
                {
                    overviewLog.Error("Test infra failure: SingleTestRun failed with " + ex);
                    Environment.Exit(-1);
                }
            });
        }

        private void RunTest()
        {
            try
            {
                RunTestMoments();

                if (!config.KeepPassedTestLogs) fixtureLog.Delete();
            }
            catch (Exception ex)
            {
                fixtureLog.Error("Test run failed with exception: " + ex);
                fixtureLog.MarkAsFailed();

                if (config.StopOnFailure)
                {
                    OverviewLog("Configured to stop on first failure. Downloading cluster logs...");
                    DownloadClusterLogs();
                    OverviewLog("Log download finished. Cancelling test runner...");
                    Cancellation.Cts.Cancel();
                }
            }
        }

        private void RunTestMoments()
        {
            var earliestMoment = handle.GetEarliestMoment();

            var t = earliestMoment;
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                RunMoment(t);

                if (handle.Test.TestFailMode == TestFailMode.StopAfterFirstFailure && exceptions.Any())
                {
                    Log("Exception detected. TestFailMode = StopAfterFirstFailure. Stopping...");
                    ThrowFailTest();
                }

                var nextMoment = handle.GetNextMoment(t);
                if (nextMoment != null)
                {
                    var delta = TimeSpan.FromSeconds(nextMoment.Value - t);
                    Log($" > Next TestMoment in {Time.FormatDuration(delta)} seconds...");
                    cancelToken.WaitHandle.WaitOne(delta);
                    t = nextMoment.Value;
                }
                else
                {
                    if (exceptions.Any())
                    {
                        ThrowFailTest();
                    }
                    OverviewLog(" > Test passed. " + FuturesInfo());
                    return;
                }
            }
        }

        private void ThrowFailTest()
        {
            var ex = UnpackException(exceptions.First());
            Log(ex.ToString());
            OverviewLog($" > Test failed {FuturesInfo()}: " + ex.Message);
            throw ex;
        }

        private string FuturesInfo()
        {
            var containers = config.CodexDeployment.CodexContainers;
            var nodes = codexNodeFactory.Create(config, containers, fixtureLog, handle.Test.TimeSet);
            var f = nodes.Select(n => n.GetDebugFutures().ToString());
            var msg = $"(Futures: [{string.Join(", ", f)}])";
            return msg;
        }

        private void DownloadClusterLogs()
        {
            var k8sFactory = new K8sFactory();
            var (_, lifecycle) = k8sFactory.CreateFacilities(config.KubeConfigFile, config.LogPath, "dataPath", config.CodexDeployment.Metadata.KubeNamespace, new DefaultTimeSet(), new NullLog(), config.RunnerLocation);

            foreach (var container in config.CodexDeployment.CodexContainers)
            {
                lifecycle.DownloadLog(container);
            }
        }

        private Exception UnpackException(Exception exception)
        {
            if (exception is AggregateException a)
            {
                return UnpackException(a.InnerExceptions.First());
            }
            if (exception is TargetInvocationException t)
            {
                return UnpackException(t.InnerException!);
            }

            return exception;
        }

        private void RunMoment(int t)
        {
            using (var context = new TestExecutionContext.IsolatedContext())
            {
                try
                {
                    handle.InvokeMoment(t, InitializeTest);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            DecommissionTest();
        }

        private void InitializeTest(string name)
        {
            Log($" > Running TestMoment '{name}'");
            handle.Test.Initialize(nodes, fixtureLog, fileManager, config, cancelToken);
        }

        private void DecommissionTest()
        {
            handle.Test.Initialize(null!, null!, null!, null!, cancelToken);
        }

        private void Log(string msg)
        {
            fixtureLog.Log(msg);
        }

        private void OverviewLog(string msg)
        {
            Log(msg);
            var containerNames = $"({string.Join(",", nodes.Select(n => n.Container.Name))})";
            overviewLog.Log($"{containerNames} {testName}: {msg}");
        }

        private CodexAccess[] CreateRandomNodes(int number)
        {
            var containers = SelectRandomContainers(number);
            fixtureLog.Log("Selected nodes: " + string.Join(",", containers.Select(c => c.Name)));
            return codexNodeFactory.Create(config, containers, fixtureLog, handle.Test.TimeSet);
        }

        private RunningContainer[] SelectRandomContainers(int number)
        {
            var containers = config.CodexDeployment.CodexContainers.ToList();
            var result = new RunningContainer[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = containers.PickOneRandom();
            }
            return result;
        }

        private DistTestCore.Configuration CreateFileManagerConfiguration()
        {
            return new DistTestCore.Configuration(null, string.Empty, false, dataFolder,
                CodexLogLevel.Error, config.RunnerLocation);
        }
    }
}
