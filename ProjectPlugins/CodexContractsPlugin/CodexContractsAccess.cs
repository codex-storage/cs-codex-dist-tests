using BlockchainUtils;
using CodexContractsPlugin.Marketplace;
using GethPlugin;
using Logging;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
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
        ulong GetPeriodNumber(DateTime utc);
        void WaitUntilNextPeriod();
        ProofState GetProofState(Request storageRequest, decimal slotIndex, ulong blockNumber, ulong period);
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

        public ICodexContractsEvents GetEvents(TimeRange timeRange)
        {
            return GetEvents(gethNode.ConvertTimeRangeToBlockRange(timeRange));
        }

        public ICodexContractsEvents GetEvents(BlockInterval blockInterval)
        {
            return new CodexContractsEvents(log, gethNode, Deployment, blockInterval);
        }

        public byte[] GetSlotId(Request request, decimal slotIndex)
        {
            var encoder = new ABIEncode();
            var encoded = encoder.GetABIEncoded(
                new ABIValue("bytes32", request.RequestId),
                new ABIValue("uint256", slotIndex.ToBig())
            );

            return Sha3Keccack.Current.CalculateHash(encoded);
        }

        public EthAddress? GetSlotHost(Request storageRequest, decimal slotIndex)
        {
            var slotId = GetSlotId(storageRequest, slotIndex);
            var func = new GetHostFunction
            {
                SlotId = slotId
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
            log.Log("Waiting until next proof period...");
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var secondsLeft = now % periodSeconds;
            Thread.Sleep(TimeSpan.FromSeconds(secondsLeft + 1));
        }

        public ProofState GetProofState(Request storageRequest, decimal slotIndex, ulong blockNumber, ulong period)
        {
            var slotId = GetSlotId(storageRequest, slotIndex);

            var required = IsProofRequired(slotId, blockNumber);
            if (!required) return new ProofState(false, false);

            var missing = IsProofMissing(slotId, blockNumber, period);
            return new ProofState(required, missing);
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
                gethNode.Call<MarkProofAsMissingFunction>(Deployment.MarketplaceAddress, funcB, blockNumber);
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
