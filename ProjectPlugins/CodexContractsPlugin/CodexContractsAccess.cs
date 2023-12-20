using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Util;
using NethereumWorkflow;
using Utils;

namespace CodexContractsPlugin
{
    public interface ICodexContracts
    {
        CodexContractsDeployment Deployment { get; }

        bool IsDeployed();
        string MintTestTokens(IHasEthAddress owner, TestToken testTokens);
        string MintTestTokens(EthAddress ethAddress, TestToken testTokens);
        TestToken GetTestTokenBalance(IHasEthAddress owner);
        TestToken GetTestTokenBalance(EthAddress ethAddress);

        Request[] GetStorageRequests(TimeRange range);
        EthAddress GetSlotHost(Request storageRequest, decimal slotIndex);
    }

    public class CodexContractsAccess : ICodexContracts
    {
        private readonly ILog log;
        private readonly IGethNode gethNode;

        public CodexContractsAccess(ILog log, IGethNode gethNode, CodexContractsDeployment deployment)
        {
            this.log = log;
            this.gethNode = gethNode;
            Deployment = deployment;
        }

        public CodexContractsDeployment Deployment { get; }

        public bool IsDeployed()
        {
            return !string.IsNullOrEmpty(StartInteraction().GetTokenName(Deployment.TokenAddress));
        }

        public string MintTestTokens(IHasEthAddress owner, TestToken testTokens)
        {
            return MintTestTokens(owner.EthAddress, testTokens);
        }

        public string MintTestTokens(EthAddress ethAddress, TestToken testTokens)
        {
            return StartInteraction().MintTestTokens(ethAddress, testTokens.Amount, Deployment.TokenAddress);
        }

        public TestToken GetTestTokenBalance(IHasEthAddress owner)
        {
            return GetTestTokenBalance(owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(EthAddress ethAddress)
        {
            var balance = StartInteraction().GetBalance(Deployment.TokenAddress, ethAddress.Address);
            return balance.TestTokens();
        }

        public Request[] GetStorageRequests(TimeRange timeRange)
        {
            var events = gethNode.GetEvents<StorageRequestedEventDTO>(Deployment.MarketplaceAddress, timeRange);
            var i = StartInteraction();

            return events
                    .Select(e =>
                    {
                        var requestEvent = i.GetRequest(Deployment.MarketplaceAddress, e.Event.RequestId);
                        var request = requestEvent.ReturnValue1;
                        request.RequestId = e.Event.RequestId;
                        return request;
                    })
                    .ToArray();
        }

        public EthAddress GetSlotHost(Request storageRequest, decimal slotIndex)
        {
            var encoder = new ABIEncode();
            var encoded = encoder.GetABIEncoded(
                new ABIValue("bytes32", storageRequest.RequestId),
                new ABIValue("uint256", slotIndex.ToBig())
            );

            var hashed = Sha3Keccack.Current.CalculateHash(encoded);

            var func = new GetHostFunction
            {
                SlotId = hashed
            };
            return new EthAddress(gethNode.Call<GetHostFunction, string>(Deployment.MarketplaceAddress, func));
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
