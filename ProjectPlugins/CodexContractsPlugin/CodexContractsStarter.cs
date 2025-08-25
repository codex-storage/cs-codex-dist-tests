using CodexClient;
using CodexContractsPlugin.Marketplace;
using Core;
using GethPlugin;
using KubernetesWorkflow;
using KubernetesWorkflow.Types;
using Logging;
using Newtonsoft.Json;
using Utils;

namespace CodexContractsPlugin
{
    public class CodexContractsStarter
    {
        private readonly IPluginTools tools;

        public CodexContractsStarter(IPluginTools tools)
        {
            this.tools = tools;
        }

        public CodexContractsDeployment Deploy(CoreInterface ci, IGethNode gethNode, DebugInfoVersion versionInfo)
        {
            Log("Starting Codex SmartContracts container...");

            var workflow = tools.CreateWorkflow();
            var startupConfig = CreateStartupConfig(gethNode);
            startupConfig.NameOverride = "codex-contracts";

            var recipe = new CodexContractsContainerRecipe(versionInfo);
            Log($"Using image: {recipe.Image}");

            var containers = workflow.Start(1, recipe, startupConfig).WaitForOnline();
            if (containers.Containers.Length != 1) throw new InvalidOperationException("Expected 1 Codex contracts container to be created. Test infra failure.");
            var container = containers.Containers[0];

            Log("Container started.");
            var watcher = workflow.CreateCrashWatcher(container);
            watcher.Start();

            try
            {
                var result = DeployContract(container, workflow, gethNode);

                workflow.Stop(containers, waitTillStopped: false);
                watcher.Stop();
                Log("Container stopped.");
                return result;
            }
            catch (Exception ex)
            {
                Log("Failed to deploy contract: " + ex);
                Log("Downloading Codex SmartContracts container log...");
                ci.DownloadLog(container);
                throw;
            }
        }

        public ICodexContracts Wrap(IGethNode gethNode, CodexContractsDeployment deployment)
        {
            return new CodexContractsAccess(tools.GetLog(), gethNode, deployment);
        }

        private CodexContractsDeployment DeployContract(RunningContainer container, IStartupWorkflow workflow, IGethNode gethNode)
        {
            Log("Deploying SmartContract...");
            WaitUntil(() =>
            {
                var logHandler = new ContractsReadyLogHandler(tools.GetLog());
                workflow.DownloadContainerLog(container, logHandler, 100);
                return logHandler.Found;
            }, nameof(DeployContract));
            Log("Contracts deployed. Extracting addresses...");

            var extractor = new ContractsContainerInfoExtractor(tools.GetLog(), workflow, container);
            var marketplaceAddress = extractor.ExtractMarketplaceAddress();
            if (string.IsNullOrEmpty(marketplaceAddress)) throw new Exception("Marketplace address not received.");
            var (abi, bytecode) = extractor.ExtractMarketplaceAbiAndByteCode();
            if (string.IsNullOrEmpty(abi)) throw new Exception("ABI not received.");
            if (string.IsNullOrEmpty(bytecode)) throw new Exception("bytecode not received.");
            EnsureCompatbility(abi, bytecode);

            var interaction = new ContractInteractions(tools.GetLog(), gethNode);
            var tokenAddress = interaction.GetTokenAddress(marketplaceAddress);
            if (string.IsNullOrEmpty(tokenAddress)) throw new Exception("Token address not received.");
            Log("TokenAddress: " + tokenAddress);

            Log("Extract completed. Checking sync...");

            Time.WaitUntil(() => interaction.IsSynced(marketplaceAddress, abi), nameof(DeployContract));

            Log("Synced. Codex SmartContracts deployed. Getting configuration...");

            var config = GetMarketplaceConfiguration(marketplaceAddress, gethNode);
            Log("Got config: " + JsonConvert.SerializeObject(config));

            ConfigShouldEqual(config.Proofs.Period, CodexContractsContainerRecipe.PeriodSeconds, "Period");
            ConfigShouldEqual(config.Proofs.Timeout, CodexContractsContainerRecipe.TimeoutSeconds, "Timeout");
            ConfigShouldEqual(config.Proofs.Downtime, CodexContractsContainerRecipe.DowntimeSeconds, "Downtime");

            return new CodexContractsDeployment(config, marketplaceAddress, abi, tokenAddress);
        }

        private void ConfigShouldEqual(ulong value, int expected, string name)
        {
            if (Convert.ToInt32(value) != expected)
            {
                throw new Exception($"Config value '{name}' should be deployed as '{expected}' but was '{value}'");
            }
        }

        private MarketplaceConfig GetMarketplaceConfiguration(string marketplaceAddress, IGethNode gethNode)
        {
            var func = new ConfigurationFunctionBase();
            var response = gethNode.Call<ConfigurationFunctionBase, ConfigurationOutputDTO>(marketplaceAddress, func);
            return response.ReturnValue1;
        }

        private void EnsureCompatbility(string abi, string bytecode)
        {
            var expectedByteCode = MarketplaceDeploymentBase.BYTECODE.ToLowerInvariant();

            if (bytecode != expectedByteCode)
            {
                Log("Deployed contract is incompatible with current build of CodexContracts plugin. Running self-updater...");
                var selfUpdater = new SelfUpdater();
                selfUpdater.Update(abi, bytecode);
            }
        }

        private void Log(string msg)
        {
            tools.GetLog().Log(msg);
        }

        private void WaitUntil(Func<bool> predicate, string msg)
        {
            Time.WaitUntil(predicate, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2), msg);
        }

        private StartupConfig CreateStartupConfig(IGethNode gethNode)
        {
            var startupConfig = new StartupConfig();
            var contractsConfig = new CodexContractsContainerConfig(gethNode);
            startupConfig.Add(contractsConfig);
            return startupConfig;
        }
    }

    public class ContractsReadyLogHandler : LogHandler
    {
        // Log should contain 'Compiled 15 Solidity files successfully' at some point.
        private const string RequiredCompiledString = "Solidity files successfully";
        // When script is done, it prints the ready-string.
        private const string ReadyString = "Done! Sleeping indefinitely...";
        private readonly ILog log;

        public ContractsReadyLogHandler(ILog log)
        {
            this.log = log;

            log.Debug($"Looking for '{RequiredCompiledString}' and '{ReadyString}' in container logs...");
        }

        public bool SeenCompileString { get; private set; }
        public bool Found { get; private set; }

        protected override void ProcessLine(string line)
        {
            log.Debug(line);
            if (line.Contains(RequiredCompiledString)) SeenCompileString = true;
            if (line.Contains(ReadyString))
            {
                if (!SeenCompileString) throw new Exception("CodexContracts deployment failed. " +
                    "Solidity files not compiled before process exited.");

                Found = true;
            }
        }
    }
}
