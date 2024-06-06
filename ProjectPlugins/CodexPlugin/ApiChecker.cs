using Core;
using KubernetesWorkflow.Types;
using Logging;
using System.Security.Cryptography;
using System.Text;

namespace CodexPlugin
{
    public class ApiChecker
    {
        // <INSERT-OPENAPI-YAML-HASH>
        private const string OpenApiYamlHash = "27-D0-F6-EB-B9-A6-66-41-AA-EA-19-62-07-AF-47-41-25-5E-75-7E-97-35-CC-E1-C0-75-58-17-2D-87-11-75";
        private const string OpenApiFilePath = "/codex/openapi.yaml";
        private const string DisableEnvironmentVariable = "CODEXPLUGIN_DISABLE_APICHECK";

        private const bool Disable = false;

        private const string Warning =
            "Warning: CodexPlugin was unable to find the openapi.yaml file in the Codex container. Are you running an old version of Codex? " +
            "Plugin will continue as normal, but API compatibility is not guaranteed!";

        private const string Failure =
            "Codex API compatibility check failed! " +
            "openapi.yaml used by CodexPlugin does not match openapi.yaml in Codex container. Please update the openapi.yaml in " +
            "'ProjectPlugins/CodexPlugin' and rebuild this project. If you wish to disable API compatibility checking, please set " +
            $"the environment variable '{DisableEnvironmentVariable}' or set the disable bool in 'ProjectPlugins/CodexPlugin/ApiChecker.cs'.";

        private static bool checkPassed = false;

        private readonly IPluginTools pluginTools;
        private readonly ILog log;

        public ApiChecker(IPluginTools pluginTools)
        {
            this.pluginTools = pluginTools;
            log = pluginTools.GetLog();

            if (string.IsNullOrEmpty(OpenApiYamlHash)) throw new Exception("OpenAPI yaml hash was not inserted by pre-build trigger.");
        }

        public void CheckCompatibility(RunningPod[] containers)
        {
            if (checkPassed) return;

            Log("CodexPlugin is checking API compatibility...");

            if (Disable || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DisableEnvironmentVariable)))
            {
                Log("API compatibility checking has been disabled.");
                checkPassed = true;
                return;
            }

            var workflow = pluginTools.CreateWorkflow();
            var container = containers.First().Containers.First();
            var containerApi = workflow.ExecuteCommand(container, "cat", OpenApiFilePath);

            if (string.IsNullOrEmpty(containerApi))
            {
                log.Error(Warning);

                checkPassed = true;
                return;
            }

            var containerHash = Hash(containerApi);
            if (containerHash == OpenApiYamlHash)
            {
                Log("API compatibility check passed.");
                checkPassed = true;
                return;
            }

            log.Error(Failure);
            throw new Exception(Failure);
        }

        private string Hash(string file)
        {
            var fileBytes = Encoding.ASCII.GetBytes(file
                .Replace(Environment.NewLine, ""));
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(fileBytes);
            return BitConverter.ToString(hash);
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
