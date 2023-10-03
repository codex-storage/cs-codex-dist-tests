using CodexPlugin;
using Core;
using DistTestCore.Logs;
using Logging;
using Newtonsoft.Json;

namespace ContinuousTests
{
    public class StartupChecker
    {
        private readonly TestFactory testFactory = new TestFactory();
        private readonly EntryPoint entryPoint;
        private readonly Configuration config;
        private readonly CancellationToken cancelToken;

        public StartupChecker(EntryPoint entryPoint, Configuration config, CancellationToken cancelToken)
        {
            this.entryPoint = entryPoint;
            this.config = config;
            this.cancelToken = cancelToken;
            LogReplacements = new List<BaseLogStringReplacement>();
        }

        public void Check()
        {
            var log = new FixtureLog(new LogConfig(config.LogPath, false), DateTime.UtcNow, "StartupChecks");
            log.Log("Starting continuous test run...");
            IncludeDeploymentConfiguration(log);
            log.Log("Checking configuration...");
            PreflightCheck(config);
            log.Log("Contacting Codex nodes...");
            CheckCodexNodes(log, config);
            log.Log("All OK.");
        }

        public List<BaseLogStringReplacement> LogReplacements { get; }

        private void IncludeDeploymentConfiguration(ILog log)
        {
            log.Log("");
            var deployment = config.CodexDeployment;
            foreach (var container in deployment.CodexContainers)
            {
                log.Log($"Codex environment variables for '{container.Name}':");
                var codexVars = container.Recipe.EnvVars;
                foreach (var vars in codexVars) log.Log(vars.ToString());
                log.Log("");
            }
            log.Log($"Deployment metadata: {JsonConvert.SerializeObject(deployment.Metadata)}");
            log.Log("");
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
            var nodes = entryPoint.CreateInterface().WrapCodexContainers(config.CodexDeployment.CodexContainers);
            var pass = true;
            foreach (var n in nodes)
            {
                cancelToken.ThrowIfCancellationRequested();

                log.Log($"Checking {n.Container.Name} @ '{n.Container.Address.Host}:{n.Container.Address.Port}'...");

                if (EnsureOnline(log, n))
                {
                    log.Log("OK");
                }
                else
                {
                    log.Error($"No response from '{n.Container.Address.Host}'.");
                    pass = false;
                }
            }
            if (!pass)
            {
                throw new Exception("Not all codex nodes responded.");
            }
        }

        private bool EnsureOnline(BaseLog log, ICodexNode n)
        {
            try
            {
                var info = n.GetDebugInfo();
                if (info == null || string.IsNullOrEmpty(info.id)) return false;

                log.Log($"Codex version: '{info.codex.version}' revision: '{info.codex.revision}'");
                LogReplacements.Add(new BaseLogStringReplacement(info.id, n.GetName()));
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
            return errors;
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
                if (test.RequiredNumberOfNodes != -1)
                {
                    if (test.RequiredNumberOfNodes < 1)
                    {
                        errors.Add($"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Test must require > 0 nodes, or -1 to select all nodes.");
                    }
                    else if (test.RequiredNumberOfNodes > config.CodexDeployment.CodexContainers.Length)
                    {
                        errors.Add($"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Deployment only has {config.CodexDeployment.CodexContainers.Length}");
                    }
                }
            }
        }
    }
}
