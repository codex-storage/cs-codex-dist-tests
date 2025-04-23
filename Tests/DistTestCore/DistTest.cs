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
    public abstract class DistTest
    {
        private const string TestNamespacePrefix = "cdx-";
        private readonly Configuration configuration = new Configuration();
        private readonly Assembly[] testAssemblies;
        private readonly FixtureLog fixtureLog;
        private readonly StatusLog statusLog;
        private readonly EntryPoint globalEntryPoint;
        private readonly DistTestLifecycleComponents lifecycleComponents = new DistTestLifecycleComponents();
        private readonly string deployId;
        
        public DistTest()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            testAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();
            
            deployId = NameUtils.MakeDeployId();

            var logConfig = configuration.GetLogConfig();
            var startTime = DateTime.UtcNow;
            fixtureLog = FixtureLog.Create(logConfig, startTime, deployId);
            statusLog = new StatusLog(logConfig, startTime, "dist-tests", deployId);

            globalEntryPoint = new EntryPoint(fixtureLog, configuration.GetK8sConfiguration(new DefaultK8sTimeSet(), TestNamespacePrefix), configuration.GetFileManagerFolder());

            Initialize(fixtureLog);
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            fixtureLog.Log($"Starting...");
            globalEntryPoint.Announce();

            // Previous test run may have been interrupted.
            // Begin by cleaning everything up.
            try
            {
                Stopwatch.Measure(fixtureLog, "Global setup", () =>
                {
                    globalEntryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(TestNamespacePrefix, wait: true);
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
                deleteTrackedFiles: true,
                waitTillDone: true
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
                try
                {
                    var testName = GetCurrentTestName();
                    fixtureLog.WriteLogTag();
                    Stopwatch.Measure(fixtureLog, $"Setup for {testName}", () =>
                    {
                        lifecycleComponents.Setup(testName, CreateComponents);
                    });
                }
                catch (Exception ex)
                {
                    fixtureLog.Error("Setup failed: " + ex);
                    GlobalTestFailure.HasFailed = true;
                }
            }
        }

        [TearDown]
        public void TearDownDistTest()
        {
            try
            {
                Stopwatch.Measure(fixtureLog, $"Teardown for {GetCurrentTestName()}", DisposeTestLifecycle);
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
                return Get().CoreInterface;
            }
        }

        public TrackedFile GenerateTestFile(ByteSize size, string label = "")
        {
            return Get().GenerateTestFile(size, label);
        }

        public TrackedFile GenerateTestFile(Action<IGenerateOption> options, string label = "")
        {
            return Get().GenerateTestFile(options, label);
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

        protected virtual void CreateComponents(ILifecycleComponentCollector collector)
        {
            var testNamespace = TestNamespacePrefix + Guid.NewGuid().ToString();
            var lifecycle = new TestLifecycle(
                fixtureLog.CreateTestLog(),
                configuration,
                GetWebCallTimeSet(),
                GetK8sTimeSet(),
                testNamespace,
                GetCurrentTestName(),
                deployId,
                ShouldWaitForCleanup());

            collector.AddComponent(lifecycle);
        }

        protected virtual void DestroyComponents(TestLifecycle lifecycle, DistTestResult testResult)
        {
        }

        public T Get<T>() where T : ILifecycleComponent
        {
            return lifecycleComponents.Get<T>(GetCurrentTestName());
        }

        private TestLifecycle Get()
        {
            return Get<TestLifecycle>();
        }

        private void DisposeTestLifecycle()
        {
            var testName = GetCurrentTestName();
            var results = GetTestResult();
            var lifecycle = Get();
            var testDuration = lifecycle.GetTestDuration();
            var data = lifecycle.GetPluginMetadata();
            fixtureLog.Log($"{GetCurrentTestName()} = {results} ({testDuration})");
            statusLog.ConcludeTest(results, testDuration, data);

            lifecycleComponents.TearDown(testName, results);
        }


        private IWebCallTimeSet GetWebCallTimeSet()
        {
            if (ShouldUseLongTimeouts()) return new LongWebCallTimeSet();
            return new DefaultWebCallTimeSet();
        }

        private IK8sTimeSet GetK8sTimeSet()
        {
            if (ShouldUseLongTimeouts()) return new LongK8sTimeSet();
            return new DefaultK8sTimeSet();
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

            var testClasses = testAssemblies.SelectMany(a => a.GetTypes()).Where(c => c.FullName == className).ToArray();
            var testMethods = testClasses.SelectMany(c => c.GetMethods()).Where(m => m.Name == methodName).ToArray();

            return testMethods.Select(m => m.GetCustomAttribute<T>())
                .Where(a => a != null)
                .Cast<T>()
                .ToArray();
        }

        private string GetCurrentTestName()
        {
            return $"[{TestContext.CurrentContext.Test.Name}]";
        }

        private DistTestResult GetTestResult()
        {
            var success = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
            var status = TestContext.CurrentContext.Result.Outcome.Status.ToString();
            var result = TestContext.CurrentContext.Result.Message;
            var trace = TestContext.CurrentContext.Result.StackTrace;
            return new DistTestResult(success, status, result ?? string.Empty, trace ?? string.Empty);
        }

        private bool IsDownloadingLogsEnabled()
        {
            return !HasDontDownloadAttribute();
        }
    }

    public class DistTestResult
    {
        public DistTestResult(bool success, string status, string result, string trace)
        {
            Success = success;
            Status = status;
            Result = result;
            Trace = trace;
        }

        public bool Success { get; }
        public string Status { get; }
        public string Result { get; }
        public string Trace { get; }

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
