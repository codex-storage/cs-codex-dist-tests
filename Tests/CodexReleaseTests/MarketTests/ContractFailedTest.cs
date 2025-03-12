using CodexClient;
using CodexContractsPlugin.ChainMonitor;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using NUnit.Framework;
using System.Numerics;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class ContractFailedTest : MarketplaceAutoBootstrapDistTest
    {
        private const int FilesizeMb = 10;
        private const int NumberOfSlots = 3;

        protected override int NumberOfHosts => 6;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => (5 * FilesizeMb).MB();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(5.0);
        private readonly TestToken pricePerBytePerSecond = 10.TstWei();

        [Test]
        public void ContractFailed()
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            var validator = StartValidator();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            request.WaitForStorageContractStarted();
            AssertContractSlotsAreFilledByHosts(request, hosts);

            hosts.Stop(waitTillStopped: true);
            
            var config = GetContracts().Deployment.Config;
            request.WaitForContractFailed(config);

            var frees = GetOnChainSlotFrees(hosts);
            Assert.That(frees.Length, Is.EqualTo(
                request.Purchase.MinRequiredNumberOfNodes - request.Purchase.NodeFailureTolerance));

            var periodReports = GetPeriodMonitorReports();
            var missedProofs = periodReports.Reports.SelectMany(r => r.MissedProofs).ToArray();
            AssertEnoughProofsWereMissedForSlotFree(frees, missedProofs, config);

            AssertClientPaidNothing(client);
            AssertValidatorWasPaidPerMissedProof(validator, request, missedProofs, config);
            AssertHostCollateralWasBurned(hosts, request);
        }

        private void AssertClientPaidNothing(ICodexNode client)
        {
            AssertTstBalance(client, StartingBalanceTST.Tst(), "Client should not have paid for failed contract.");
        }

        private void AssertValidatorWasPaidPerMissedProof(ICodexNode validator, IStoragePurchaseContract request, PeriodProofMissed[] missedProofs, MarketplaceConfig config)
        {
            var rewardPerMissedProof = GetValidatorRewardPerMissedProof(request, config);
            var totalValidatorReward = rewardPerMissedProof * missedProofs.Length;

            AssertTstBalance(validator, StartingBalanceTST.Tst() + totalValidatorReward, $"Validator is rewarded per slot marked as missing. " +
                $"numberOfMissedProofs: {missedProofs.Length} rewardPerMissedProof: {rewardPerMissedProof}");
        }

        private TestToken GetCollatoralPerSlot(IStoragePurchaseContract request)
        {
            var slotSize = new ByteSize(Convert.ToInt64(request.GetStatus()!.Request.Ask.SlotSize));
            return new TestToken(request.Purchase.CollateralPerByte.TstWei * slotSize.SizeInBytes);
        }

        private void AssertHostCollateralWasBurned(ICodexNodeGroup hosts, IStoragePurchaseContract request)
        {
            var slotFills = GetOnChainSlotFills(hosts);
            foreach (var host in hosts)
            {
                AssertHostCollateralWasBurned(host, slotFills, request);
            }
        }

        private void AssertHostCollateralWasBurned(ICodexNode host, SlotFill[] slotFills, IStoragePurchaseContract request)
        {
            // In case of a failed contract, the entire slotColateral is lost.
            var filledByHost = slotFills.Where(f => f.Host.EthAddress == host.EthAddress).ToArray();
            var numSlotsOfHost = filledByHost.Length;
            var collatoralPerSlot = GetCollatoralPerSlot(request);
            var totalCost = collatoralPerSlot * numSlotsOfHost;

            AssertTstBalance(host, StartingBalanceTST.Tst() - totalCost, $"Host has lost collateral for each slot. " +
                $"numberOfSlotsByHost: {numSlotsOfHost} collateralPerSlot: {collatoralPerSlot}");
        }

        private TestToken GetValidatorRewardPerMissedProof(IStoragePurchaseContract request, MarketplaceConfig config)
        {
            var collatoralPerSlot = GetCollatoralPerSlot(request);
            var slashPercentage = config.Collateral.SlashPercentage;
            var validatorRewardPercentage = config.Collateral.ValidatorRewardPercentage;

            var rewardPerMissedProof =
                PercentageOf(
                    PercentageOf(collatoralPerSlot, slashPercentage),
                    validatorRewardPercentage);

            return rewardPerMissedProof;
        }

        private TestToken PercentageOf(TestToken value, byte percentage)
        {
            var p = new BigInteger(percentage);
            return new TestToken((value.TstWei * p) / 100);
        }

        private void AssertEnoughProofsWereMissedForSlotFree(SlotFree[] frees, PeriodProofMissed[] missedProofs, MarketplaceConfig config)
        {
            foreach (var free in frees)
            {
                AssertEnoughProofsWereMissedForSlotFree(free, missedProofs, config);
            }
        }

        private void AssertEnoughProofsWereMissedForSlotFree(SlotFree free, PeriodProofMissed[] missedProofs, MarketplaceConfig config)
        {
            var missedByHost = missedProofs.Where(p => p.Host != null && p.Host.Address == free.Host.EthAddress.Address).ToArray();
            var maxNumMissedProofsBeforeFreeSlot = config.Collateral.MaxNumberOfSlashes;
            Assert.That(missedByHost.Length, Is.EqualTo(maxNumMissedProofsBeforeFreeSlot));
        }

        private TimeSpan CalculateContractFailTimespan()
        {
            var config = GetContracts().Deployment.Config;
            var requiredNumMissedProofs = Convert.ToInt32(config.Collateral.MaxNumberOfSlashes);
            var periodDuration = GetPeriodDuration();

            // Each host could miss 1 proof per period,
            // so the time we should wait is period time * requiredNum of missed proofs.
            // Except: the proof requirement has a concept of "downtime":
            // a segment of time where proof is not required.
            // We calculate the probability of downtime and extend the waiting
            // timeframe by a factor, such that all hosts are highly likely to have 
            // failed a sufficient number of proofs.

            float n = requiredNumMissedProofs;
            return periodDuration * n * GetDowntimeFactor(config);
        }

        private float GetDowntimeFactor(MarketplaceConfig config)
        {
            byte numBlocksInDowntimeSegment = config.Proofs.Downtime;
            float downtime = numBlocksInDowntimeSegment;
            float window = 256.0f;
            var chanceOfDowntime = downtime / window;
            return 1.0f + chanceOfDowntime + chanceOfDowntime;
        }

        private IStoragePurchaseContract CreateStorageRequest(ICodexNode client)
        {
            var cid = client.UploadFile(GenerateTestFile(FilesizeMb.MB()));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = TimeSpan.FromMinutes(20.0),
                Expiry = TimeSpan.FromMinutes(5.0),
                MinRequiredNumberOfNodes = NumberOfSlots,
                NodeFailureTolerance = 1,
                PricePerBytePerSecond = pricePerBytePerSecond,
                ProofProbability = 1, // Require a proof every period
                CollateralPerByte = 1.TstWei()
            });
        }
    }
}
