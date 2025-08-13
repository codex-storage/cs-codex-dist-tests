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
        void WaitUntilNextPeriod();
        ProofState GetProofState(byte[] requestId, decimal slotIndex, ulong period);
        byte[] GetSlotId(byte[] requestId, decimal slotIndex);

        ICodexContracts WithDifferentGeth(IGethNode node);
    }

    public enum ProofState
    {
        NotRequired,
        NotMissed,
        MissedNotMarked,
        MissedAndMarked,
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

        public void WaitUntilNextPeriod()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var periodSeconds = (int)Deployment.Config.Proofs.Period;
            var secondsLeft = now % periodSeconds;
            Thread.Sleep(TimeSpan.FromSeconds(secondsLeft + 1));
        }

        public ProofState GetProofState(byte[] requestId, decimal slotIndex, ulong period)
        {
            var slotId = GetSlotId(requestId, slotIndex);

            var required = IsProofRequired(slotId);
            if (!required) return ProofState.NotRequired;

            return IsProofMissing(slotId, period);
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

        private ProofState IsProofMissing(byte[] slotId, ulong period)
        {
            // In case of a missed proof, one of two things can be true:
            // 1 - The proof was missed but no validator marked it as missing:
            //     We can see this by calling "canMarkProofAsMissing" and it returns true/doesn't throw.
            // 2 - The proof was missed and it was marked as missing by a validator:
            //     We can see this by a successful call to "MarkProofAsMissing" on-chain.

            if (CallCanMarkProofAsMissing(slotId, period))
            {
                return ProofState.MissedNotMarked;
            }
            if (WasMarkProofAsMissingCalled(slotId, period))
            {
                return ProofState.MissedAndMarked;
            }

            return ProofState.NotMissed;
        }

        private bool CallCanMarkProofAsMissing(byte[] slotId, ulong period)
        {
            try
            {
                var func = new CanMarkProofAsMissingFunction
                {
                    SlotId = slotId,
                    Period = period
                };

                gethNode.Call<CanMarkProofAsMissingFunction>(Deployment.MarketplaceAddress, func);

                return true;
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
        }

        private bool WasMarkProofAsMissingCalled(byte[] slotId, ulong period)
        {
            var now = DateTime.UtcNow;
            var currentPeriod = new TimeRange(now - Deployment.Config.PeriodDuration, now);
            var interval = gethNode.ConvertTimeRangeToBlockRange(currentPeriod);
            var slot = slotId.ToHex().ToLowerInvariant();

            var found = false;
            gethNode.IterateFunctionCalls<MarkProofAsMissingFunction>(interval, (b, fn) =>
            {
                if (fn.Period == period && fn.SlotId.ToHex().ToLowerInvariant() == slot)
                {
                    found = true;
                }
            });

            return found;
        }

        private ContractInteractions StartInteraction()
        {
            return new ContractInteractions(log, gethNode);
        }
    }
}
