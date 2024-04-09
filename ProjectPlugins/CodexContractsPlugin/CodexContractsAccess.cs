using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Hex.HexTypes;
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

        Request[] GetStorageRequests(BlockInterval blockRange);
        EthAddress? GetSlotHost(Request storageRequest, decimal slotIndex);
        RequestState GetRequestState(Request request);
        RequestFulfilledEventDTO[] GetRequestFulfilledEvents(BlockInterval blockRange);
        RequestCancelledEventDTO[] GetRequestCancelledEvents(BlockInterval blockRange);
        SlotFilledEventDTO[] GetSlotFilledEvents(BlockInterval blockRange);
        SlotFreedEventDTO[] GetSlotFreedEvents(BlockInterval blockRange);
    }

    public enum RequestState
    {
        New,
        Started,
        Cancelled,
        Finished,
        Failed
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

        public Request[] GetStorageRequests(BlockInterval blockRange)
        {
            var events = gethNode.GetEvents<StorageRequestedEventDTO>(Deployment.MarketplaceAddress, blockRange);
            var i = StartInteraction();
            return events
                    .Select(e =>
                    {
                        var requestEvent = i.GetRequest(Deployment.MarketplaceAddress, e.Event.RequestId);
                        var result = requestEvent.ReturnValue1;
                        result.BlockNumber = e.Log.BlockNumber.ToUlong();
                        result.RequestId = e.Event.RequestId;
                        return result;
                    })
                    .ToArray();
        }

        public RequestFulfilledEventDTO[] GetRequestFulfilledEvents(BlockInterval blockRange)
        {
            var events = gethNode.GetEvents<RequestFulfilledEventDTO>(Deployment.MarketplaceAddress, blockRange);
            return events.Select(e =>
            {
                var result = e.Event;
                result.BlockNumber = e.Log.BlockNumber.ToUlong();
                return result;
            }).ToArray();
        }

        public RequestCancelledEventDTO[] GetRequestCancelledEvents(BlockInterval blockRange)
        {
            var events = gethNode.GetEvents<RequestCancelledEventDTO>(Deployment.MarketplaceAddress, blockRange);
            return events.Select(e =>
            {
                var result = e.Event;
                result.BlockNumber = e.Log.BlockNumber.ToUlong();
                return result;
            }).ToArray();
        }

        public SlotFilledEventDTO[] GetSlotFilledEvents(BlockInterval blockRange)
        {
            var events = gethNode.GetEvents<SlotFilledEventDTO>(Deployment.MarketplaceAddress, blockRange);
            return events.Select(e =>
            {
                var result = e.Event;
                result.BlockNumber = e.Log.BlockNumber.ToUlong();
                result.Host = GetEthAddressFromTransaction(e.Log.TransactionHash);
                return result;
            }).ToArray();
        }

        public SlotFreedEventDTO[] GetSlotFreedEvents(BlockInterval blockRange)
        {
            var events = gethNode.GetEvents<SlotFreedEventDTO>(Deployment.MarketplaceAddress, blockRange);
            return events.Select(e =>
            {
                var result = e.Event;
                result.BlockNumber = e.Log.BlockNumber.ToUlong();
                return result;
            }).ToArray();
        }

        public EthAddress? GetSlotHost(Request storageRequest, decimal slotIndex)
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
            var address = gethNode.Call<GetHostFunction, string>(Deployment.MarketplaceAddress, func);
            if (string.IsNullOrEmpty(address)) return null;
            return new EthAddress(address);
        }

        public RequestState GetRequestState(Request request)
        {
            var func = new RequestStateFunction
            {
                RequestId = request.RequestId
            };
            return gethNode.Call<RequestStateFunction, RequestState>(Deployment.MarketplaceAddress, func);
        }

        private EthAddress GetEthAddressFromTransaction(string transactionHash)
        {
            var transaction = gethNode.GetTransaction(transactionHash);
            return new EthAddress(transaction.From);
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
