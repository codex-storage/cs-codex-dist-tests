using DistTestCore.Codex;
using DistTestCore;
using Logging;

namespace ContinuousTests
{
    public class StartupChecker
    {
        private readonly TestFactory testFactory = new TestFactory();
        private readonly CodexNodeFactory codexNodeFactory = new CodexNodeFactory();
        private readonly Configuration config;

        public StartupChecker(Configuration config)
        {
            this.config = config;
        }

        public void Check()
        {
            var log = new FixtureLog(new LogConfig(config.LogPath, false), "StartupChecks");
            log.Log("Starting continuous test run...");
            log.Log("Checking configuration...");
            PreflightCheck(config);
            log.Log("Contacting Codex nodes...");
            CheckCodexNodes(log, config);
            log.Log("All OK.");
        }

        private void PreflightCheck(Configuration config)
        {
            var tests = testFactory.CreateTests();
            if (!tests.Any())
            {
                throw new Exception("Unable to find any tests.");
            }
            foreach (var test in tests)
            {
                var handle = new TestHandle(test);
                handle.GetEarliestMoment();
                handle.GetLastMoment();
            }

            var errors = new List<string>();
            foreach (var test in tests)
            {
                if (test.RequiredNumberOfNodes > config.CodexDeployment.CodexContainers.Length)
                {
                    errors.Add($"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Deployment only has {config.CodexDeployment.CodexContainers.Length}");
                }
            }

            if (!Directory.Exists(config.LogPath))
            {
                Directory.CreateDirectory(config.LogPath);
            }

            if (errors.Any())
            {
                throw new Exception("Prerun check failed: " + string.Join(", ", errors));
            }
        }

        private void CheckCodexNodes(BaseLog log, Configuration config)
        {
            var nodes = codexNodeFactory.Create(config.CodexDeployment.CodexContainers, log, new DefaultTimeSet());
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
