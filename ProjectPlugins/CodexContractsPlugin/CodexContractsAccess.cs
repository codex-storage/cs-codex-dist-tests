using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
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
        string TransferTestTokens(EthAddress to, TestToken amount);

        ICodexContractsEvents GetEvents(TimeRange timeRange);
        ICodexContractsEvents GetEvents(BlockInterval blockInterval);
        EthAddress? GetSlotHost(byte[] requestId, decimal slotIndex);
        RequestState GetRequestState(byte[] requestId);
        Request GetRequest(byte[] requestId);
        ulong GetPeriodNumber(DateTime utc);
        TimeRange GetPeriodTimeRange(ulong periodNumber);
        void WaitUntilNextPeriod();
        bool IsProofRequired(byte[] requestId, decimal slotIndex);
        byte[] GetSlotId(byte[] requestId, decimal slotIndex);

        ICodexContracts WithDifferentGeth(IGethNode node);
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

        public string TransferTestTokens(EthAddress to, TestToken amount)
        {
            return StartInteraction().TransferTestTokens(Deployment.TokenAddress, to.Address, amount.TstWei);
        }

        public ICodexContractsEvents GetEvents(TimeRange timeRange)
        {
            return GetEvents(gethNode.ConvertTimeRangeToBlockRange(timeRange));
        }

        public ICodexContractsEvents GetEvents(BlockInterval blockInterval)
        {
            return new CodexContractsEvents(log, gethNode, Deployment, blockInterval);
        }

        public EthAddress? GetSlotHost(byte[] requestId, decimal slotIndex)
        {
            var slotId = GetSlotId(requestId, slotIndex);
            var func = new GetHostFunction
            {
                SlotId = slotId
            };
            var address = gethNode.Call<GetHostFunction, string>(Deployment.MarketplaceAddress, func);
            if (string.IsNullOrEmpty(address)) return null;
            return new EthAddress(address);
        }

        public RequestState GetRequestState(byte[] requestId)
        {
            if (requestId == null) throw new ArgumentNullException(nameof(requestId));
            if (requestId.Length != 32) throw new InvalidDataException(nameof(requestId) + $"{nameof(requestId)} length should be 32 bytes, but was: {requestId.Length}" + requestId.Length);

            var func = new RequestStateFunction
            {
                RequestId = requestId
            };
            return gethNode.Call<RequestStateFunction, RequestState>(Deployment.MarketplaceAddress, func);
        }

        public Request GetRequest(byte[] requestId)
        {
            if (requestId == null) throw new ArgumentNullException(nameof(requestId));
            if (requestId.Length != 32) throw new InvalidDataException(nameof(requestId) + $"{nameof(requestId)} length should be 32 bytes, but was: {requestId.Length}" + requestId.Length);
            var func = new GetRequestFunction
            {
                RequestId = requestId
            };

            var request = gethNode.Call<GetRequestFunction, GetRequestOutputDTO>(Deployment.MarketplaceAddress, func);
            return request.ReturnValue1;
        }

        public ulong GetPeriodNumber(DateTime utc)
        {
            DateTimeOffset utco = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var now = utco.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var result = now / periodSeconds;
            return Convert.ToUInt64(result);
        }

        public TimeRange GetPeriodTimeRange(ulong periodNumber)
        {
            var periodSeconds = (ulong)Deployment.Config.Proofs.Period;
            var startUtco = Convert.ToInt64(periodSeconds * periodNumber);
            var endUtco = Convert.ToInt64(periodSeconds * (periodNumber + 1));
            var start = DateTimeOffset.FromUnixTimeSeconds(startUtco).UtcDateTime;
            var end = DateTimeOffset.FromUnixTimeSeconds(endUtco).UtcDateTime;
            return new TimeRange(start, end);
        }

        public void WaitUntilNextPeriod()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var secondsLeft = now % periodSeconds;
            Thread.Sleep(TimeSpan.FromSeconds(secondsLeft + 1));
        }

        public bool IsProofRequired(byte[] requestId, decimal slotIndex)
        {
            var slotId = GetSlotId(requestId, slotIndex);
            return IsProofRequired(slotId);
        }

        public ICodexContracts WithDifferentGeth(IGethNode node)
        {
            return new CodexContractsAccess(log, node, Deployment);
        }

        public byte[] GetSlotId(byte[] requestId, decimal slotIndex)
        {
            var encoder = new ABIEncode();
            var encoded = encoder.GetABIEncoded(
                new ABIValue("bytes32", requestId),
                new ABIValue("uint256", slotIndex.ToBig())
            );

            return Sha3Keccack.Current.CalculateHash(encoded);
        }

        private bool IsProofRequired(byte[] slotId)
        {
            var func = new IsProofRequiredFunction
            {
                Id = slotId
            };
            var result = gethNode.Call<IsProofRequiredFunction, IsProofRequiredOutputDTO>(Deployment.MarketplaceAddress, func);
            return result.ReturnValue1;
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
