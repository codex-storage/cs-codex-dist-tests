using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexDeployment
    {
        public CodexDeployment(GethStartResult gethStartResult, RunningContainer[] codexContainers, RunningContainer? prometheusContainer, DeploymentMetadata metadata)
        {
            GethStartResult = gethStartResult;
            CodexContainers = codexContainers;
            PrometheusContainer = prometheusContainer;
            Metadata = metadata;
        }

        public GethStartResult GethStartResult { get; }
        public RunningContainer[] CodexContainers { get; }
        public RunningContainer? PrometheusContainer { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string codexImage, string gethImage, string contractsImage, string prometheusImage, string kubeNamespace, int numberOfCodexNodes, int numberOfValidators, int storageQuotaMB, CodexLogLevel codexLogLevel, int initialTestTokens, int minPrice, int maxCollateral, int maxDuration)
        {
            DeployDateTimeUtc = DateTime.UtcNow;
            CodexImage = codexImage;
            GethImage = gethImage;
            ContractsImage = contractsImage;
            PrometheusImage = prometheusImage;
            KubeNamespace = kubeNamespace;
            NumberOfCodexNodes = numberOfCodexNodes;
            NumberOfValidators = numberOfValidators;
            StorageQuotaMB = storageQuotaMB;
            CodexLogLevel = codexLogLevel;
            InitialTestTokens = initialTestTokens;
            MinPrice = minPrice;
            MaxCollateral = maxCollateral;
            MaxDuration = maxDuration;
        }

        public string CodexImage { get; }
        public DateTime DeployDateTimeUtc { get; }
        public string GethImage { get; }
        public string ContractsImage { get; }
        public string PrometheusImage { get; }
        public string KubeNamespace { get; }
        public int NumberOfCodexNodes { get; }
        public int NumberOfValidators { get; }
        public int StorageQuotaMB { get; }
        public CodexLogLevel CodexLogLevel { get; }
        public int InitialTestTokens { get; }
        public int MinPrice { get; }
        public int MaxCollateral { get; }
        public int MaxDuration { get; }
    }
}
