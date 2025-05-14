using System.Diagnostics;
using System.Reflection;
using Core;
using Logging;

namespace DistTestCore
{
    public class Global
    {
        public const string TestNamespacePrefix = "cdx-";
        public Configuration Configuration { get; } = new Configuration();

        public Assembly[] TestAssemblies { get; }
        private readonly EntryPoint globalEntryPoint;
        private readonly ILog log;

        public Global()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            TestAssemblies = assemblies.Where(a => a.FullName!.ToLowerInvariant().Contains("test")).ToArray();

            log = new ConsoleLog();
            globalEntryPoint = new EntryPoint(
                log,
                Configuration.GetK8sConfiguration(
                    new DefaultK8sTimeSet(),
                    TestNamespacePrefix
                ),
                Configuration.GetFileManagerFolder()
            );
        }

        public void Setup()
        {
            try
            {
                Trace.Listeners.Add(new ConsoleTraceListener());

                Logging.Stopwatch.Measure(log, "Global setup", () =>
                {
                    globalEntryPoint.Announce();
                    globalEntryPoint.Tools.CreateWorkflow().DeleteNamespacesStartingWith(TestNamespacePrefix, wait: true);
                });
            }
            catch (Exception ex)
            {
                GlobalTestFailure.HasFailed = true;
                log.Error($"Global setup cleanup failed with: {ex}");
                throw;
            }
        }

        public void TearDown()
        {
            globalEntryPoint.Decommission(
                // There shouldn't be any of either, but clean everything up regardless.
                deleteKubernetesResources: true,
                deleteTrackedFiles: true,
                waitTillDone: true
            );

            Trace.Flush();
        }
    }
}
