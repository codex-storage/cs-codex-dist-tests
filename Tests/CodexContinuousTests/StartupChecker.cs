using CodexClient;
using Core;
using DistTestCore.Logs;
using Logging;

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
            var log = FixtureLog.Create(new LogConfig(config.LogPath), DateTime.UtcNow, config.CodexDeployment.Id,
                "StartupChecks");
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

            throw new NotImplementedException();
            //var deployment = config.CodexDeployment;
            //var workflow = entryPoint.Tools.CreateWorkflow();
            //foreach (var instance in deployment.CodexInstances)
            //{
            //    foreach (var container in instance.Pod.Containers)
            //    {
            //        var podInfo = workflow.GetPodInfo(container);
            //        log.Log($"Codex environment variables for '{container.Name}':");
            //        log.Log(
            //            $"Namespace: {container.RunningPod.StartResult.Cluster.Configuration.KubernetesNamespace} - " +
            //            $"Pod name: {podInfo.Name} - Deployment name: {instance.Pod.StartResult.Deployment.Name}");
            //        var codexVars = container.Recipe.EnvVars;
            //        foreach (var vars in codexVars) log.Log(vars.ToString());
            //        log.Log("");
            //    }
            //}

            //log.Log($"Deployment metadata: {JsonConvert.SerializeObject(deployment.Metadata)}");
            //log.Log("");
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

        private void CheckCodexNodes(ILog log, Configuration config)
        {
            throw new NotImplementedException();

            //var nodes = entryPoint.CreateInterface()
            //    .WrapCodexContainers(config.CodexDeployment.CodexInstances.Select(i => i.Pod).ToArray());
            //var pass = true;
            //foreach (var n in nodes)
            //{
            //    cancelToken.ThrowIfCancellationRequested();

            //    var address = n.GetApiEndpoint();
            //    log.Log($"Checking {n.GetName()} @ '{address}'...");

            //    if (EnsureOnline(log, n))
            //    {
            //        log.Log("OK");
            //    }
            //    else
            //    {
            //        log.Error($"No response from '{address}'.");
            //        pass = false;
            //    }
            //}

            //if (!pass)
            //{
            //    throw new Exception("Not all codex nodes responded.");
            //}
        }

        private bool EnsureOnline(BaseLog log, ICodexNode n)
        {
            try
            {
                var info = n.GetDebugInfo();
                if (info == null || string.IsNullOrEmpty(info.Id)) return false;

                log.Log($"Codex version: '{info.Version.Version}' revision: '{info.Version.Revision}'");
                LogReplacements.Add(new BaseLogStringReplacement(info.Id, n.GetName()));
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

        private void DuplicatesCheck(ContinuousTest[] tests, List<string> errors,
            Func<ContinuousTest, bool> considerCondition, Func<ContinuousTest, object> getValue, string propertyName)
        {
            foreach (var test in tests)
            {
                if (considerCondition(test))
                {
                    var duplicates = tests.Where(t => t != test && getValue(t) == getValue(test)).ToList();
                    if (duplicates.Any())
                    {
                        duplicates.Add(test);
                        errors.Add(
                            $"Tests '{string.Join(",", duplicates.Select(d => d.Name))}' have the same '{propertyName}'. These must be unique.");
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
                        errors.Add(
                            $"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Test must require > 0 nodes, or -1 to select all nodes.");
                    }
                    else if (test.RequiredNumberOfNodes > config.CodexDeployment.CodexInstances.Length)
                    {
                        errors.Add(
                            $"Test '{test.Name}' requires {test.RequiredNumberOfNodes} nodes. Deployment only has {config.CodexDeployment.CodexInstances.Length}");
                    }
                }
            }
        }
    }
}