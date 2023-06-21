using DistTestCore;
using DistTestCore.Codex;
using Logging;
using Utils;

namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly ConfigLoader configLoader = new ConfigLoader();
        private readonly TestFinder testFinder = new TestFinder();

        public void Run()
        {
            var config = configLoader.Load();
            var log = new TestLog(config.LogPath, true);

            log.Log("Starting continuous test run...");
            log.Log("Checking configuration...");
            PreflightCheck(config);
            log.Log("Contacting Codex nodes...");
            var nodes = CreateCodexNodes(log, new LongTimeSet(), config);
            log.Log("OK");
            log.Log("");

            while (true)
            {
                var run = new TestRun(config, log, testFinder, nodes);

                try
                {
                    run.Run();
                }
                catch (Exception ex)
                {
                    log.Error($"Exception during test run: " + ex);
                }

                Thread.Sleep(config.SleepSecondsPerTest * 1000);
            }
        }

        private void PreflightCheck(Configuration config)
        {
            var tests = testFinder.GetTests();
            if (!tests.Any())
            {
                throw new Exception("Unable to find any tests.");
            }

            var errors = new List<string>();
            foreach (var test in tests)
            {
                if (test.RequiredNumberOfNodes > config.CodexUrls.Length)
                {
                    errors.Add($"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Configuration only has {config.CodexUrls.Length}");
                }
            }

            if (errors.Any())
            {
                throw new Exception("Prerun check failed: " + string.Join(", ", errors));
            }
        }

        private CodexNode[] CreateCodexNodes(BaseLog log, ITimeSet timeSet, Configuration config)
        {
            var nodes = config.CodexUrls.Select(url =>
            {
                var address = new Address(url, 1234);
                return new CodexNode(log, timeSet, address);
            }).ToArray();

            var pass = true;
            foreach (var n in nodes)
            {
                log.Log($"Checking '{n.Address.Host}'...");

                if (EnsureOnline(n))
                {
                    log.Log("OK");
                }
                else
                {
                    log.Error($"No response from '{n.Address.Host}'.");
                    pass = false;
                }
            }
            if (!pass)
            {
                throw new Exception("Not all codex nodes responded.");
            }

            return nodes;
        }

        private bool EnsureOnline(CodexNode n)
        {
            try
            {
                var info = n.GetDebugInfo();
                if (info == null || string.IsNullOrEmpty(info.id)) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
