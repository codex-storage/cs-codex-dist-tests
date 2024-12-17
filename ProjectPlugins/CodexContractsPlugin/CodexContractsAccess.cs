using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NethereumWorkflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        ICodexContractsEvents GetEvents(TimeRange timeRange);
        ICodexContractsEvents GetEvents(BlockInterval blockInterval);
        EthAddress? GetSlotHost(Request storageRequest, decimal slotIndex);
        RequestState GetRequestState(Request request);
        void WaitUntilNextPeriod();
    }

    [JsonConverter(typeof(StringEnumConverter))]
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
            return StartInteraction().MintTestTokens(ethAddress, testTokens.TstWei, Deployment.TokenAddress);
        }

        public TestToken GetTestTokenBalance(IHasEthAddress owner)
        {
            return GetTestTokenBalance(owner.EthAddress);
        }

        public TestToken GetTestTokenBalance(EthAddress ethAddress)
        {
            var balance = StartInteraction().GetBalance(Deployment.TokenAddress, ethAddress.Address);
            return balance.TstWei();
        }

        public ICodexContractsEvents GetEvents(TimeRange timeRange)
        {
            return GetEvents(gethNode.ConvertTimeRangeToBlockRange(timeRange));
        }

        public ICodexContractsEvents GetEvents(BlockInterval blockInterval)
        {
            return new CodexContractsEvents(log, gethNode, Deployment, blockInterval);
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

        public void WaitUntilNextPeriod()
        {
            log.Log("Waiting until next proof period...");
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var secondsLeft = now % periodSeconds;
            Thread.Sleep(TimeSpan.FromSeconds(secondsLeft + 1));
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
