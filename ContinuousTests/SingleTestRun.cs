using DistTestCore.Codex;
using DistTestCore;
using Logging;
using Utils;
using KubernetesWorkflow;
using NUnit.Framework.Internal;

namespace ContinuousTests
{
    public class SingleTestRun
    {
        private readonly CodexNodeFactory codexNodeFactory = new CodexNodeFactory();
        private readonly List<Exception> exceptions = new List<Exception>();
        private readonly Configuration config;
        private readonly BaseLog overviewLog;
        private readonly TestHandle handle;
        private readonly CodexNode[] nodes;
        private readonly FileManager fileManager;
        private readonly FixtureLog fixtureLog;
        private readonly string testName;
        private readonly string dataFolder;

        public SingleTestRun(Configuration config, BaseLog overviewLog, TestHandle handle)
        {
            this.config = config;
            this.overviewLog = overviewLog;
            this.handle = handle;

            testName = handle.Test.GetType().Name;
            fixtureLog = new FixtureLog(new LogConfig(config.LogPath, false), testName);

            nodes = CreateRandomNodes(handle.Test.RequiredNumberOfNodes);
            dataFolder = config.DataPath + "-" + Guid.NewGuid();
            fileManager = new FileManager(fixtureLog, CreateFileManagerConfiguration());
        }

        public void Run()
        {
            Task.Run(() =>
            {
                try
                {
                    RunTest();

                    if (!config.KeepPassedTestLogs) fixtureLog.Delete();
                }
                catch (Exception ex)
                {
                    fixtureLog.Error("Test run failed with exception: " + ex);
                    fixtureLog.MarkAsFailed();
                }
                fileManager.DeleteAllTestFiles();
                Directory.Delete(dataFolder, true);
            });
        }

        private void RunTest()
        {
            var earliestMoment = handle.GetEarliestMoment();

            var t = earliestMoment;
            while (true)
            {
                RunMoment(t);

                if (handle.Test.TestFailMode == TestFailMode.StopAfterFirstFailure && exceptions.Any())
                {
                    Log("Exception detected. TestFailMode = StopAfterFirstFailure. Stopping...");
                    throw exceptions.Single();
                }

                var nextMoment = handle.GetNextMoment(t);
                if (nextMoment != null)
                {
                    Log($" > Next TestMoment in {nextMoment.Value} seconds...");
                    t += nextMoment.Value;
                    Thread.Sleep(nextMoment.Value * 1000);
                }
                else
                {
                    if (exceptions.Any())
                    {
                        var ex = exceptions.First();
                        OverviewLog(" > Test failed: " + ex);
                        throw ex;
                    }
                    OverviewLog(" > Test passed.");
                    return;
                }
            }
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
                    Log($" > TestMoment yielded exception: " + ex);
                    exceptions.Add(ex);
                }
            }

            DecommissionTest();
        }

        private void InitializeTest(string name)
        {
            Log($" > Running TestMoment '{name}'");
            handle.Test.Initialize(nodes, fixtureLog, fileManager, config);
        }

        private void DecommissionTest()
        {
            handle.Test.Initialize(null!, null!, null!, null!);
        }

        private void Log(string msg)
        {
            fixtureLog.Log(msg);
        }

        private void OverviewLog(string msg)
        {
            Log(msg);
            overviewLog.Log(testName + ": " +  msg);
        }

        private CodexNode[] CreateRandomNodes(int number)
        {
            var containers = SelectRandomContainers(number);
            fixtureLog.Log("Selected nodes: " + string.Join(",", containers.Select(c => c.Name)));
            return codexNodeFactory.Create(containers, fixtureLog, handle.Test.TimeSet);
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
                CodexLogLevel.Error, TestRunnerLocation.ExternalToCluster);
        }
    }
}
