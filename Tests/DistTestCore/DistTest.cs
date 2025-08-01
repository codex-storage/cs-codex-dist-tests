using Core;
using DistTestCore.Logs;
using FileUtils;
using Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Reflection;
using Utils;
using WebUtils;
using Assert = NUnit.Framework.Assert;

namespace DistTestCore
{
    [Parallelizable(ParallelScope.All)]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public abstract class DistTest
    {
        private static readonly Global global = new Global();
        private readonly FixtureLog fixtureLog;
        private readonly StatusLog statusLog;
        private readonly TestLifecycle lifecycle;
        private readonly string deployId = NameUtils.MakeDeployId();

        public DistTest()
        {
            var logConfig = global.Configuration.GetLogConfig();
            var startTime = DateTime.UtcNow;
            fixtureLog = FixtureLog.Create(logConfig, startTime, deployId);
            statusLog = new StatusLog(logConfig, startTime, "dist-tests", deployId);

            fixtureLog.Log("Test framework revision: " + GitInfo.GetStatus());

            lifecycle = new TestLifecycle(fixtureLog.CreateTestLog(startTime), global.Configuration,
                GetWebCallTimeSet(),
                GetK8sTimeSet(),
                Global.TestNamespacePrefix + Guid.NewGuid().ToString(),
                deployId,
                ShouldWaitForCleanup()
            );

            Initialize(fixtureLog);
        }

        [OneTimeSetUp]
        public static void GlobalSetup()
        {
            global.Setup();
        }

        [OneTimeTearDown]
        public static void GlobalTearDown()
        {
            global.TearDown();
        }

        [SetUp]
        public void SetUpDistTest()
        {
            if (GlobalTestFailure.HasFailed)
            {
                Assert.Inconclusive("Skip test: Previous test failed during clean up.");
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
                fixtureLog.Error("Cleanup failed: " + ex);
                GlobalTestFailure.HasFailed = true;
            }
        }

        public CoreInterface Ci
        {
            get
            {
                return lifecycle.CoreInterface;
            }
        }

        public TrackedFile GenerateTestFile(ByteSize size, string label = "")
        {
            return lifecycle.GenerateTestFile(size, label);
        }

        public TrackedFile GenerateTestFile(Action<IGenerateOption> options, string label = "")
        {
            return lifecycle.GenerateTestFile(options, label);
        }

        /// <summary>
        /// Any test files generated in 'action' will be deleted after it returns.
        /// This helps prevent large tests from filling up discs.
        /// </summary>
        public void ScopedTestFiles(Action action)
        {
            lifecycle.GetFileManager().ScopedFiles(action);
        }

        public ILog GetTestLog()
        {
            return lifecycle.Log;
        }

        public IFileManager GetFileManager()
        {
            return lifecycle.GetFileManager();
        }

        public string GetTestNamespace()
        {
            return lifecycle.TestNamespace;
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
            Stopwatch.Measure(lifecycle.Log, name, action);
        }

        protected TimeRange GetTestRunTimeRange()
        {
            return new TimeRange(lifecycle.TestStartUtc, DateTime.UtcNow);
        }

        protected virtual void Initialize(FixtureLog fixtureLog)
        {
        }

        protected virtual void CollectStatusLogData(TestLifecycle lifecycle, Dictionary<string, string> data)
        {
        }

        private void DisposeTestLifecycle()
        {
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
                lifecycle.DeleteAllResources();
            });
        }

        private void WriteEndTestLog(TestLog log)
        {
            var result = TestContext.CurrentContext.Result;

            Log($"*** Finished: {GetCurrentTestName()} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{Environment.NewLine}{result.StackTrace}");
            }
        }

        private IWebCallTimeSet GetWebCallTimeSet()
        {
            if (IsRunningInCluster())
            {
                Log(" > Detected we're running in the cluster. Using long webCall timeset.");
                return new LongWebCallTimeSet();
            }

            if (ShouldUseLongTimeouts()) return new LongWebCallTimeSet();
            return new DefaultWebCallTimeSet();
        }

        private IK8sTimeSet GetK8sTimeSet()
        {
            if (IsRunningInCluster())
            {
                Log(" > Detected we're running in the cluster. Using long kubernetes timeset.");
                return new LongK8sTimeSet();
            }

            if (ShouldUseLongTimeouts()) return new LongK8sTimeSet();
            return new DefaultK8sTimeSet();
        }

        private bool IsRunningInCluster()
        {
            var testType = Environment.GetEnvironmentVariable("TEST_TYPE");
            return testType == "release-tests";
        }

        private bool ShouldWaitForCleanup()
        {
            return CurrentTestMethodHasAttribute<WaitForCleanupAttribute>();
        }

        private bool ShouldUseLongTimeouts()
        {
            return CurrentTestMethodHasAttribute<UseLongTimeoutsAttribute>();
        }

        private bool HasDontDownloadAttribute()
        {
            return CurrentTestMethodHasAttribute<DontDownloadLogsAttribute>();
        }

        protected bool CurrentTestMethodHasAttribute<T>() where T : PropertyAttribute
        {
            return GetCurrentTestMethodAttribute<T>().Any();
        }

        protected T[] GetCurrentTestMethodAttribute<T>() where T : PropertyAttribute
        {
            // Don't be fooled! TestContext.CurrentTest.Test allows you easy access to the attributes of the current test.
            // But this doesn't work for tests making use of [TestCase] or [Combinatorial]. So instead, we use reflection here to
            // fetch the attributes of type T.
            var currentTest = TestContext.CurrentContext.Test;
            var className = currentTest.ClassName;
            var methodName = currentTest.MethodName;

            var testClasses = global.TestAssemblies.SelectMany(a => a.GetTypes()).Where(c => c.FullName == className).ToArray();
            var testMethods = testClasses.SelectMany(c => c.GetMethods()).Where(m => m.Name == methodName).ToArray();

            return testMethods.Select(m => m.GetCustomAttribute<T>())
                .Where(a => a != null)
                .Cast<T>()
                .ToArray();
        }

        protected IDownloadedLog[] DownloadAllLogs()
        {
            return lifecycle.DownloadAllLogs();
        }

        private void IncludeLogsOnTestFailure(TestLifecycle lifecycle)
        {
            var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
            if (ShouldDownloadAllLogs(testStatus))
            {
                lifecycle.Log.Log("Downloading all container logs...");
                DownloadAllLogs();
            }
        }

        private bool ShouldDownloadAllLogs(TestStatus testStatus)
        {
            if (global.Configuration.AlwaysDownloadContainerLogs) return true;
            if (!IsDownloadingLogsEnabled()) return false;
            if (testStatus == TestStatus.Failed)
            {
                return true;
            }

            return false;
        }

        private string GetCurrentTestName()
        {
            return $"[{NameUtils.GetRawFixtureName()}:{NameUtils.GetTestMethodName()}]";
        }

        public DistTestResult GetTestResult()
        {
            var success = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
            var status = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            var result = TestContext.CurrentContext.Result.Message;
            return new DistTestResult(success, status, result ?? string.Empty);
        }

        private bool IsDownloadingLogsEnabled()
        {
            return !HasDontDownloadAttribute();
        }
    }

    public class DistTestResult
    {
        public DistTestResult(bool success, string status, string result)
        {
            Success = success;
            Status = status;
            Result = result;
        }

        public bool Success { get; }
        public string Status { get; }
        public string Result { get; }

        public override string ToString()
        {
            if (Success) return $"Passed ({Status}) ({Result})";
            return $"Failed ({Status}) ({Result})";
        }
    }

    public static class GlobalTestFailure
    {
        public static bool HasFailed { get; set; } = false;
    }
}
