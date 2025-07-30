using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.Contracts;
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
        void TransferTestTokens(EthAddress to, TestToken amount);

        ICodexContractsEvents GetEvents(TimeRange timeRange);
        ICodexContractsEvents GetEvents(BlockInterval blockInterval);
        EthAddress? GetSlotHost(byte[] requestId, decimal slotIndex);
        RequestState GetRequestState(byte[] requestId);
        Request GetRequest(byte[] requestId);
        ulong GetPeriodNumber(DateTime utc);
        void WaitUntilNextPeriod();
        ProofState GetProofState(byte[] requestId, decimal slotIndex, ulong blockNumber, ulong period);

        ICodexContracts WithDifferentGeth(IGethNode node);
    }

    public class ProofState
    {
        public ProofState(bool required, bool missing)
        {
            Required = required;
            Missing = missing;
        }

        public bool Required { get; }
        public bool Missing { get; }
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

        public void TransferTestTokens(EthAddress to, TestToken amount)
        {
            StartInteraction().TransferTestTokens(Deployment.TokenAddress, to.Address, amount.TstWei);
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

        public void WaitUntilNextPeriod()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var secondsLeft = now % periodSeconds;
            Thread.Sleep(TimeSpan.FromSeconds(secondsLeft + 1));
        }

        public ProofState GetProofState(byte[] requestId, decimal slotIndex, ulong blockNumber, ulong period)
        {
            var slotId = GetSlotId(requestId, slotIndex);

            var required = IsProofRequired(slotId, blockNumber);
            if (!required) return new ProofState(false, false);

            var missing = IsProofMissing(slotId, blockNumber, period);
            return new ProofState(required, missing);
        }

        public ICodexContracts WithDifferentGeth(IGethNode node)
        {
            return new CodexContractsAccess(log, node, Deployment);
        }

        private byte[] GetSlotId(byte[] requestId, decimal slotIndex)
        {
            var encoder = new ABIEncode();
            var encoded = encoder.GetABIEncoded(
                new ABIValue("bytes32", requestId),
                new ABIValue("uint256", slotIndex.ToBig())
            );

            return Sha3Keccack.Current.CalculateHash(encoded);
        }

        private bool IsProofRequired(byte[] slotId, ulong blockNumber)
        {
            var func = new IsProofRequiredFunction
            {
                Id = slotId
            };
            var result = gethNode.Call<IsProofRequiredFunction, IsProofRequiredOutputDTO>(Deployment.MarketplaceAddress, func, blockNumber);
            return result.ReturnValue1;
        }

        private bool IsProofMissing(byte[] slotId, ulong blockNumber, ulong period)
        {
            try
            {
                var funcB = new MarkProofAsMissingFunction
                {
                    SlotId = slotId,
                    Period = period
                };
                gethNode.Call(Deployment.MarketplaceAddress, funcB, blockNumber);
            }
            catch (AggregateException exc)
            {
                if (exc.InnerExceptions.Count == 1)
                {
                    if (exc.InnerExceptions[0].GetType() == typeof(SmartContractCustomErrorRevertException))
                    {
                        return false;
                    }
                }
                throw;
            }
            return true;
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
