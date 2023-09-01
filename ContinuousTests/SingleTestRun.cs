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
        private static int failureCount = 0;

        public SingleTestRun(TaskFactory taskFactory, Configuration config, BaseLog overviewLog, TestHandle handle, StartupChecker startupChecker, CancellationToken cancelToken)
        {
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.handle = handle;
            this.cancelToken = cancelToken;
            testName = handle.Test.GetType().Name;
            fixtureLog = new FixtureLog(new LogConfig(config.LogPath, true), DateTime.UtcNow, testName);
            ApplyLogReplacements(fixtureLog, startupChecker);

            nodes = CreateRandomNodes();
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

                failureCount++;
                if (config.StopOnFailure > 0)
                {
                    OverviewLog($"Failures: {failureCount} / {config.StopOnFailure}");
                    if (failureCount >= config.StopOnFailure)
                    {
                        OverviewLog($"Configured to stop after {config.StopOnFailure} failures. Downloading cluster logs...");
                        DownloadClusterLogs();
                        OverviewLog("Log download finished. Cancelling test runner...");
                        Cancellation.Cts.Cancel();
                    }
                }
            }
        }

        private void ApplyLogReplacements(FixtureLog fixtureLog, StartupChecker startupChecker)
        {
            foreach (var replacement in startupChecker.LogReplacements) fixtureLog.AddStringReplace(replacement.From, replacement.To);
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
                    OverviewLog(" > Test passed.");
                    return;
                }
            }
        }

        private void ThrowFailTest()
        {
            var exs = UnpackExceptions(exceptions);
            var exceptionsMessage = GetCombinedExceptionsMessage(exs);
            Log(exceptionsMessage);
            OverviewLog($" > Test failed: " + exceptionsMessage);
            throw new Exception(exceptionsMessage);
        }

        private void DownloadClusterLogs()
        {
            var k8sFactory = new K8sFactory();
            var lifecycle = k8sFactory.CreateTestLifecycle(config.KubeConfigFile, config.LogPath, "dataPath", config.CodexDeployment.Metadata.KubeNamespace, new DefaultTimeSet(), new NullLog());

            foreach (var container in config.CodexDeployment.CodexContainers)
            {
                lifecycle.DownloadLog(container);
            }
        }

        private string GetCombinedExceptionsMessage(Exception[] exceptions)
        {
            return string.Join(Environment.NewLine, exceptions.Select(ex => ex.ToString()));
        }

        private Exception[] UnpackExceptions(List<Exception> exceptions)
        {
            return exceptions.Select(UnpackException).ToArray();
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
            var containerNames = GetContainerNames();
            overviewLog.Log($"{containerNames} {testName}: {msg}");
        }

        private string GetContainerNames()
        {
            if (handle.Test.RequiredNumberOfNodes == -1) return "(All Nodes)";
            return $"({string.Join(",", nodes.Select(n => n.Container.Name))})";
        }

        private CodexAccess[] CreateRandomNodes()
        {
            var containers = SelectRandomContainers();
            fixtureLog.Log("Selected nodes: " + string.Join(",", containers.Select(c => c.Name)));
            return codexNodeFactory.Create(config, containers, fixtureLog, handle.Test.TimeSet);
        }

        private RunningContainer[] SelectRandomContainers()
        {
            var number = handle.Test.RequiredNumberOfNodes;
            if (number == -1) return config.CodexDeployment.CodexContainers;

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
                CodexLogLevel.Error, string.Empty);
        }
    }
}
