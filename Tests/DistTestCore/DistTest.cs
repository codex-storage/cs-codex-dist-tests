using Core;
using DistTestCore.Logs;
using FileUtils;
using Logging;
using NUnit.Framework;
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
        
        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();

            var logConfig = configuration.GetLogConfig();
            var startTime = DateTime.UtcNow;
            fixtureLog = new FixtureLog(logConfig, startTime);
            statusLog = new StatusLog(logConfig, startTime, "dist-tests");

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

        protected virtual void Initialize(FixtureLog fixtureLog)
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
                    var lifecycle = new TestLifecycle(fixtureLog.CreateTestLog(), configuration, GetTimeSet(), testNamespace);
                    lifecycles.Add(testName, lifecycle);
                }
            });
        }

        private void DisposeTestLifecycle()
        {
            var lifecycle = Get();
            var testResult = GetTestResult();
            var testDuration = lifecycle.GetTestDuration();
            fixtureLog.Log($"{GetCurrentTestName()} = {testResult} ({testDuration})");
            statusLog.ConcludeTest(testResult, testDuration, lifecycle.GetPluginMetadata());
            Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", () =>
            {
                WriteEndTestLog(lifecycle.Log);

                IncludeLogsOnTestFailure(lifecycle);
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

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
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
            var result = TestContext.CurrentContext.Result;
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                fixtureLog.MarkAsFailed();

                if (IsDownloadingLogsEnabled())
                {
                    lifecycle.Log.Log("Downloading all container logs because of test failure...");
                    lifecycle.DownloadAllLogs();
                }
                else
                {
                    lifecycle.Log.Log("Skipping download of all container logs due to [DontDownloadLogsOnFailure] attribute.");
                }
            }
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
