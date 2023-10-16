using Logging;
using Utils;
using KubernetesWorkflow;
using NUnit.Framework.Internal;
using System.Reflection;
using CodexPlugin;
using DistTestCore.Logs;
using Core;

namespace ContinuousTests
{
    public class SingleTestRun
    {
        private readonly List<Exception> exceptions = new List<Exception>();
        private readonly EntryPoint entryPoint;
        private readonly TaskFactory taskFactory;
        private readonly Configuration config;
        private readonly ILog overviewLog;
        private readonly TestHandle handle;
        private readonly CancellationToken cancelToken;
        private readonly ICodexNode[] nodes;
        private readonly FixtureLog fixtureLog;
        private readonly string testName;
        private static int failureCount = 0;

        public SingleTestRun(EntryPointFactory entryPointFactory, TaskFactory taskFactory, Configuration config, ILog overviewLog, TestHandle handle, StartupChecker startupChecker, CancellationToken cancelToken)
        {
            this.taskFactory = taskFactory;
            this.config = config;
            this.overviewLog = overviewLog;
            this.handle = handle;
            this.cancelToken = cancelToken;
            testName = handle.Test.GetType().Name;
            fixtureLog = new FixtureLog(new LogConfig(config.LogPath, false), DateTime.UtcNow, testName);
            entryPoint = entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, fixtureLog);
            ApplyLogReplacements(fixtureLog, startupChecker);

            nodes = CreateRandomNodes();
        }

        public void Run(EventWaitHandle runFinishedHandle, Action<bool> resultHandler)
        {
            taskFactory.Run(() =>
            {
                try
                {
                    RunTest(resultHandler);

                    entryPoint.Decommission(
                        deleteKubernetesResources: false, // This would delete the continuous test net.
                        deleteTrackedFiles: true
                    );
                    runFinishedHandle.Set();
                }
                catch (Exception ex)
                {
                    overviewLog.Error("Test infra failure: SingleTestRun failed with " + ex);
                    Environment.Exit(-1);
                }
            });
        }

        private void RunTest(Action<bool> resultHandler)
        {
            var testStart = DateTime.UtcNow;
            
            try
            {
                RunTestMoments();

                var duration = DateTime.UtcNow - testStart;
                OverviewLog($" > Test passed. ({Time.FormatDuration(duration)})");

                if (!config.KeepPassedTestLogs)
                {
                    fixtureLog.Delete();
                }
                resultHandler(true);
            }
            catch (Exception ex)
            {
                fixtureLog.Error("Test run failed with exception: " + ex);
                fixtureLog.MarkAsFailed();

                DownloadContainerLogs(testStart);

                failureCount++;
                resultHandler(false);
                if (config.StopOnFailure > 0)
                {
                    OverviewLog($"Failures: {failureCount} / {config.StopOnFailure}");
                    if (failureCount >= config.StopOnFailure)
                    {
                        OverviewLog($"Configured to stop after {config.StopOnFailure} failures.");
                        Cancellation.Cts.Cancel();
                    }
                }
            }
        }

        private void DownloadContainerLogs(DateTime testStart)
        {
            // The test failed just now. We can't expect the logs to be available in elastic-search immediately:
            Thread.Sleep(TimeSpan.FromMinutes(1));

            var effectiveStart = testStart.Subtract(TimeSpan.FromSeconds(30));
            if (config.FullContainerLogs)
            {
                effectiveStart = config.CodexDeployment.Metadata.StartUtc.Subtract(TimeSpan.FromSeconds(30));
            }
            var effectiveEnd = DateTime.UtcNow;
            var elasticSearchLogDownloader = new ElasticSearchLogDownloader(entryPoint.Tools, fixtureLog);

            foreach (var node in nodes)
            {
                var openingLine = $"{node.Container.Pod.PodInfo.Name} = {node.Container.Name} = {node.GetDebugInfo().id}";
                elasticSearchLogDownloader.Download(fixtureLog.CreateSubfile(), node.Container, effectiveStart, effectiveEnd, openingLine);
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
            while (!cancelToken.IsCancellationRequested)
            {
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
                    return;
                }
            }
            fixtureLog.Log("Test run has been cancelled.");
        }

        private void ThrowFailTest()
        {
            var exs = UnpackExceptions(exceptions);
            var exceptionsMessage = GetCombinedExceptionsMessage(exs);
            Log(exceptionsMessage);
            OverviewLog($" > Test failed: " + exceptionsMessage);
            throw new Exception(exceptionsMessage);
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
            handle.Test.Initialize(nodes, fixtureLog, entryPoint.Tools.GetFileManager(), config, cancelToken);
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

        private ICodexNode[] CreateRandomNodes()
        {
            var containers = SelectRandomContainers();
            fixtureLog.Log("Selected nodes: " + string.Join(",", containers.Select(c => c.Name)));
            return entryPoint.CreateInterface().WrapCodexContainers(containers).ToArray();
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
    }
}
