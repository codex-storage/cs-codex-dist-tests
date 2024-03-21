using CodexContractsPlugin;
using GethPlugin;
using KubernetesWorkflow.Types;

namespace CodexPlugin
{
    public class CodexDeployment
    {
        public CodexDeployment(CodexInstance[] codexInstances, GethDeployment gethDeployment,
            CodexContractsDeployment codexContractsDeployment, RunningContainers? prometheusContainer,
            RunningContainers? discordBotContainer, DeploymentMetadata metadata,
            string id)
        {
            Id = id;
            CodexInstances = codexInstances;
            GethDeployment = gethDeployment;
            CodexContractsDeployment = codexContractsDeployment;
            PrometheusContainer = prometheusContainer;
            DiscordBotContainer = discordBotContainer;
            Metadata = metadata;
        }

        public string Id { get; }
        public CodexInstance[] CodexInstances { get; }
        public GethDeployment GethDeployment { get; }
        public CodexContractsDeployment CodexContractsDeployment { get; }
        public RunningContainers? PrometheusContainer { get; }
        public RunningContainers? DiscordBotContainer { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class CodexInstance
    {
        public CodexInstance(RunningContainers containers, CodexDebugResponse info)
        {
            Containers = containers;
            Info = info;
        }

        public RunningContainers Containers { get; }
        public CodexDebugResponse Info { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string name, DateTime startUtc, DateTime finishedUtc, string kubeNamespace,
            int numberOfCodexNodes, int numberOfValidators, int storageQuotaMB, CodexLogLevel codexLogLevel,
            int initialTestTokens, int minPrice, int maxCollateral, int maxDuration, int blockTTL, int blockMI,
            int blockMN)
        {
            Name = name;
            StartUtc = startUtc;
            FinishedUtc = finishedUtc;
            KubeNamespace = kubeNamespace;
            NumberOfCodexNodes = numberOfCodexNodes;
            NumberOfValidators = numberOfValidators;
            StorageQuotaMB = storageQuotaMB;
            CodexLogLevel = codexLogLevel;
            InitialTestTokens = initialTestTokens;
            MinPrice = minPrice;
            MaxCollateral = maxCollateral;
            MaxDuration = maxDuration;
            BlockTTL = blockTTL;
            BlockMI = blockMI;
            BlockMN = blockMN;
        }

        public string Name { get; }
        public DateTime StartUtc { get; }
        public DateTime FinishedUtc { get; }
        public string KubeNamespace { get; }
        public int NumberOfCodexNodes { get; }
        public int NumberOfValidators { get; }
        public int StorageQuotaMB { get; }
        public CodexLogLevel CodexLogLevel { get; }
        public int InitialTestTokens { get; }
        public int MinPrice { get; }
        public int MaxCollateral { get; }
        public int MaxDuration { get; }
        public int BlockTTL { get; }
        public int BlockMI { get; }
        public int BlockMN { get; }
    }
}