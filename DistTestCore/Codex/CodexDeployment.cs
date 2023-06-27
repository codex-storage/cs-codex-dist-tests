using DistTestCore.Marketplace;
using KubernetesWorkflow;

namespace DistTestCore.Codex
{
    public class CodexDeployment
    {
        public CodexDeployment(GethStartResult gethStartResult, RunningContainer[] codexContainers, DeploymentMetadata metadata)
        {
            GethStartResult = gethStartResult;
            CodexContainers = codexContainers;
            Metadata = metadata;
        }

        public GethStartResult GethStartResult { get; }
        public RunningContainer[] CodexContainers { get; }
        public DeploymentMetadata Metadata { get; }
    }

    public class DeploymentMetadata
    {
        public DeploymentMetadata(string codexImage, string gethImage, string contractsImage, string kubeNamespace, int numberOfCodexNodes, int numberOfValidators, int storageQuotaMB, CodexLogLevel codexLogLevel)
        {
            DeployDateTimeUtc = DateTime.UtcNow;
            CodexImage = codexImage;
            GethImage = gethImage;
            ContractsImage = contractsImage;
            KubeNamespace = kubeNamespace;
            NumberOfCodexNodes = numberOfCodexNodes;
            NumberOfValidators = numberOfValidators;
            StorageQuotaMB = storageQuotaMB;
            CodexLogLevel = codexLogLevel;
        }
        
        public string CodexImage { get; }
        public DateTime DeployDateTimeUtc { get; }
        public string GethImage { get; }
        public string ContractsImage { get; }
        public string KubeNamespace { get; }
        public int NumberOfCodexNodes { get; }
        public int NumberOfValidators { get; }
        public int StorageQuotaMB { get; }
        public CodexLogLevel CodexLogLevel { get; }
    }
}
