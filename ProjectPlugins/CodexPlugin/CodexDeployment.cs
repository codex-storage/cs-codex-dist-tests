using GethPlugin;
using KubernetesWorkflow;

namespace CodexPlugin
{
    public class CodexDeployment
    {
        public CodexDeployment(RunningContainer[] codexContainers, GethDeployment gethDeployment, RunningContainer? prometheusContainer, DeploymentMetadata metadata)
        {
            CodexContainers = codexContainers;
            GethDeployment = gethDeployment;
            PrometheusContainer = prometheusContainer;
            Metadata = metadata;
        }

        public RunningContainer[] CodexContainers { get; }
        public GethDeployment GethDeployment { get; }
        public RunningContainer? PrometheusContainer { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string kubeNamespace, int numberOfCodexNodes, int numberOfValidators, int storageQuotaMB, CodexLogLevel codexLogLevel, int initialTestTokens, int minPrice, int maxCollateral, int maxDuration, int blockTTL, int blockMI, int blockMN)
        {
            DeployDateTimeUtc = DateTime.UtcNow;
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

        public DateTime DeployDateTimeUtc { get; }
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
