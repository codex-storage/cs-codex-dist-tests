﻿using Core;
using DistTestCore.Logs;
using FileUtils;
using Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Reflection;
using Utils;
using Assert = NUnit.Framework.Assert;

namespace DistTestCore
{
    [Parallelizable(ParallelScope.All)]
    public abstract class DistTest
    {
        private const string TestNamespacePrefix = "ct-";
        private readonly Configuration configuration = new Configuration();
        private readonly Assembly[] testAssemblies;
        private readonly FixtureLog fixtureLog;
        private readonly StatusLog statusLog;
        private readonly object lifecycleLock = new object();
        private readonly EntryPoint globalEntryPoint;
        private readonly Dictionary<string, TestLifecycle> lifecycles = new Dictionary<string, TestLifecycle>();
        private readonly string deployId;
        
        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();
            
            deployId = NameUtils.MakeDeployId();

            var logConfig = configuration.GetLogConfig();
            var startTime = DateTime.UtcNow;
            fixtureLog = new FixtureLog(logConfig, startTime, deployId);
            statusLog = new StatusLog(logConfig, startTime, "dist-tests", deployId);

            globalEntryPoint = new EntryPoint(fixtureLog, configuration.GetK8sConfiguration(new DefaultTimeSet(), TestNamespacePrefix), configuration.GetFileManagerFolder());

            Initialize(fixtureLog);
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            fixtureLog.Log($"Distributed Tests are starting...");
            globalEntryPoint.Announce();

            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    globalEntryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(TestNamespacePrefix);
                });                
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                fixtureLog.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }

            fixtureLog.Log("Test framework revision: " + GitInfo.GetStatus());
            fixtureLog.Log("Global setup cleanup successful");
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            globalEntryPoint.Decommission(
                // There shouldn't be any of either, but clean everything up regardless.
                deleteKubernetesResources: true,
                deleteTrackedFiles: true
            );
        }

        [SetUp]
        public void SetUpDistTest()
        {
            if (GlobalTestFailure.HasFailed)
            {
                Assert.Inconclusive("Skip test: Previous test failed during clean up.");
            }
            else
            {
                CreateNewTestLifecycle();
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                DisposeTestLifecycle();
            }
            catch (Exception ex)
            {
                fixtureLog.Error("Cleanup failed: " + ex.Message);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public CoreInterface Ci
        {
            get
            {
                return Get().CoreInterface;
            }
        }

        public TrackedFile GenerateTestFile(ByteSize size, string label = "")
        {
            return Get().GenerateTestFile(size, label);
        }

        /// <summary>
        /// Any test files generated in 'action' will be deleted after it returns.
        /// This helps prevent large tests from filling up discs.
        /// </summary>
        public void ScopedTestFiles(Action action)
        {
            Get().GetFileManager().ScopedFiles(action);
        }

        public ILog GetTestLog()
        {
            return Get().Log;
        }

        public void Log(string msg)
        {
            TestContext.Progress.WriteLine(msg);
            GetTestLog().Log(msg);
        }

        public void Debug(string msg)
        {
            TestContext.Progress.WriteLine(msg);
            GetTestLog().Debug(msg);
        }

        public void Measure(string name, Action action)
        {
            Stopwatch.Measure(Get().Log, name, action);
        }

        protected TimeRange GetTestRunTimeRange()
        {
            return new TimeRange(Get().TestStart, DateTime.UtcNow);
        }

        protected virtual void Initialize(FixtureLog fixtureLog)
        {
        }

        protected virtual void LifecycleStart(TestLifecycle lifecycle)
        {
        }

        protected virtual void LifecycleStop(TestLifecycle lifecycle)
        {
        }

        protected virtual void CollectStatusLogData(TestLifecycle lifecycle, Dictionary<string, string> data)
        {
        }

        protected TestLifecycle Get()
        {
            lock (lifecycleLock)
            {
                return lifecycles[GetCurrentTestName()];
            }
        }

        private void CreateNewTestLifecycle()
        {
            var testName = GetCurrentTestName();
            fixtureLog.WriteLogTag();
            Stopwatch.Measure(fixtureLog, $"Setup for {testName}", () =>
            {
                lock (lifecycleLock)
                {
                    var testNamespace = TestNamespacePrefix + Guid.NewGuid().ToString();
                    var lifecycle = new TestLifecycle(fixtureLog.CreateTestLog(), configuration, GetTimeSet(), testNamespace, deployId);
                    lifecycles.Add(testName, lifecycle);
                    LifecycleStart(lifecycle);
                }
            });
        }

        private void DisposeTestLifecycle()
        {
            var lifecycle = Get();
            var testResult = GetTestResult();
            var testDuration = lifecycle.GetTestDuration();
            var data = lifecycle.GetPluginMetadata();
            CollectStatusLogData(lifecycle, data);
            fixtureLog.Log($"{GetCurrentTestName()} = {testResult} ({testDuration})");
            statusLog.ConcludeTest(testResult, testDuration, data);
            Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", () =>
            {
                WriteEndTestLog(lifecycle.Log);

                IncludeLogsOnTestFailure(lifecycle);
                LifecycleStop(lifecycle);
                lifecycle.DeleteAllResources();
                lifecycle = null!;
            });
        }

        private void WriteEndTestLog(TestLog log)
        {
            var result = TestContext.CurrentContext.Result;

            Log($"*** Finished: {GetCurrentTestName()} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{result.StackTrace}");
            }

            if (result.Outcome.Status == TestStatus.Failed)
            {
                log.MarkAsFailed();
            }
        }

        private ITimeSet GetTimeSet()
        {
            if (ShouldUseLongTimeouts()) return new LongTimeSet();
            return new DefaultTimeSet();
        }

        private bool ShouldUseLongTimeouts()
        {
            // Don't be fooled! TestContext.CurrentTest.Test allows you easy access to the attributes of the current test.
            // But this doesn't work for tests making use of [TestCase]. So instead, we use reflection here to figure out
            // if the attribute is present.
            var currentTest = TestContext.CurrentContext.Test;
            var className = currentTest.ClassName;
            var methodName = currentTest.MethodName;

            var testClasses = testAssemblies.SelectMany(a => a.GetTypes()).Where(c => c.FullName == className).ToArray();
            var testMethods = testClasses.SelectMany(c => c.GetMethods()).Where(m => m.Name == methodName).ToArray();

            return testMethods.Any(m => m.GetCustomAttribute<UseLongTimeoutsAttribute>() != null);
        }

        private void IncludeLogsOnTestFailure(TestLifecycle lifecycle)
        {
            var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
            if (testStatus == TestStatus.Failed)
            {
                fixtureLog.MarkAsFailed();
            }

            if (ShouldDownloadAllLogs(testStatus))
            {
                lifecycle.Log.Log("Downloading all container logs...");
                lifecycle.DownloadAllLogs();
            }
        }

        private bool ShouldDownloadAllLogs(TestStatus testStatus)
        {
            if (configuration.AlwaysDownloadContainerLogs) return true;
            if (testStatus == TestStatus.Failed)
            {
                return IsDownloadingLogsEnabled();
            }

            return false;
        }

        private string GetCurrentTestName()
        {
            return $"[{TestContext.CurrentContext.Test.Name}]";
        }

        private string GetTestResult()
        {
            return TestContext.CurrentContext.Result.Outcome.Status.ToString();
        }

        private bool IsDownloadingLogsEnabled()
        {
            var testProperties = TestContext.CurrentContext.Test.Properties;
            return !testProperties.ContainsKey(DontDownloadLogsOnFailureAttribute.DontDownloadKey);
        }
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
