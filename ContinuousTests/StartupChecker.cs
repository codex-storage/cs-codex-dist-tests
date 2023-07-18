using DistTestCore.Codex;
using DistTestCore;
using Logging;

namespace ContinuousTests
{
    public class StartupChecker
    {
        private readonly TestFactory testFactory = new TestFactory();
        private readonly CodexAccessFactory codexNodeFactory = new CodexAccessFactory();
        private readonly Configuration config;
        private readonly CancellationToken cancelToken;

        public StartupChecker(Configuration config, CancellationToken cancelToken)
        {
            this.config = config;
            this.cancelToken = cancelToken;
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
                cancelToken.ThrowIfCancellationRequested();

                var handle = new TestHandle(test);
                handle.GetEarliestMoment();
                handle.GetLastMoment();
            }

            if (!Directory.Exists(config.LogPath))
            {
                Directory.CreateDirectory(config.LogPath);
            }

            var errors = CheckTests(tests);
            if (errors.Any())
            {
                throw new Exception("Prerun check failed: " + string.Join(", ", errors));
            }
        }

        private void CheckCodexNodes(BaseLog log, Configuration config)
        {
            var nodes = codexNodeFactory.Create(config, config.CodexDeployment.CodexContainers, log, new DefaultTimeSet());
            var pass = true;
            foreach (var n in nodes)
            {
                cancelToken.ThrowIfCancellationRequested();

                log.Log($"Checking {n.Container.Name} @ '{n.Address.Host}:{n.Address.Port}'...");

                if (EnsureOnline(log, n))
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

        private bool EnsureOnline(BaseLog log, CodexAccess n)
        {
            try
            {
                var info = n.GetDebugInfo();
                if (info == null || string.IsNullOrEmpty(info.id)) return false;

                log.Log($"Codex version: '{info.codex.version}' revision: '{info.codex.revision}'");
            }
            catch
            {
                return false;
            }
            return true;
        }

        private List<string> CheckTests(ContinuousTest[] tests)
        {
            var errors = new List<string>();
            CheckRequiredNumberOfNodes(tests, errors);
            CheckCustomNamespaceClashes(tests, errors);
            CheckEthereumIndexClashes(tests, errors);
            return errors;
        }

        private void CheckEthereumIndexClashes(ContinuousTest[] tests, List<string> errors)
        {
            var offLimits = config.CodexDeployment.CodexContainers.Length;
            foreach (var test in tests)
            {
                if (test.EthereumAccountIndex != -1)
                {
                    if (test.EthereumAccountIndex <= offLimits)
                    {
                        errors.Add($"Test '{test.Name}' has selected 'EthereumAccountIndex' = {test.EthereumAccountIndex}. All accounts up to and including {offLimits} are being used by the targetted Codex net. Select a different 'EthereumAccountIndex'.");
                    }
                }
            }

            DuplicatesCheck(tests, errors,
                considerCondition: t => t.EthereumAccountIndex != -1,
                getValue: t => t.EthereumAccountIndex,
                propertyName: nameof(ContinuousTest.EthereumAccountIndex));
        }

        private void CheckCustomNamespaceClashes(ContinuousTest[] tests, List<string> errors)
        {
            DuplicatesCheck(tests, errors,
                considerCondition: t => !string.IsNullOrEmpty(t.CustomK8sNamespace),
                getValue: t => t.CustomK8sNamespace,
                propertyName: nameof(ContinuousTest.CustomK8sNamespace));
        }

        private void DuplicatesCheck(ContinuousTest[] tests, List<string> errors, Func<ContinuousTest, bool> considerCondition, Func<ContinuousTest, object> getValue, string propertyName)
        {
            foreach (var test in tests)
            {
                if (considerCondition(test))
                {
                    var duplicates = tests.Where(t => t != test && getValue(t) == getValue(test)).ToList();
                    if (duplicates.Any())
                    {
                        duplicates.Add(test);
                        errors.Add($"Tests '{string.Join(",", duplicates.Select(d => d.Name))}' have the same '{propertyName}'. These must be unique.");
                        return;
                    }
                }
            }
        }

        private void CheckRequiredNumberOfNodes(ContinuousTest[] tests, List<string> errors)
        {
            foreach (var test in tests)
            {
                if (test.RequiredNumberOfNodes > config.CodexDeployment.CodexContainers.Length)
                {
                    errors.Add($"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Deployment only has {config.CodexDeployment.CodexContainers.Length}");
                }
            }
        }
    }
}
