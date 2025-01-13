using CodexContractsPlugin;
using CodexContractsPlugin.Marketplace;
using CodexPlugin;
using CodexTests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    public class ContractFailedTest : MarketplaceAutoBootstrapDistTest
    {
        protected override int NumberOfHosts => 4;
        protected override int NumberOfClients => 1;
        protected override ByteSize HostAvailabilitySize => 1.GB();
        protected override TimeSpan HostAvailabilityMaxDuration => TimeSpan.FromDays(1.0);
        private readonly TestToken pricePerSlotPerSecond = 10.TstWei();

        [Test]
        [Ignore("Disabled for now: Test is unstable.")]
        public void ContractFailed()
        {
            var hosts = StartHosts();
            var client = StartClients().Single();
            StartValidator();

            var request = CreateStorageRequest(client);

            request.WaitForStorageContractSubmitted();
            AssertContractIsOnChain(request);

            request.WaitForStorageContractStarted();
            AssertContractSlotsAreFilledByHosts(request, hosts);

            hosts.BringOffline(waitTillStopped: true);

            WaitForSlotFreedEvents();

            request.WaitForContractFailed();
        }

        private void WaitForSlotFreedEvents()
        {
            Log(nameof(WaitForSlotFreedEvents));

            var start = DateTime.UtcNow;
            var timeout = CalculateContractFailTimespan();

            while (DateTime.UtcNow < start + timeout)
            {
                var events = GetContracts().GetEvents(GetTestRunTimeRange());
                var slotFreed = events.GetSlotFreedEvents();
                if (slotFreed.Length == NumberOfHosts)
                {
                    Log($"{nameof(WaitForSlotFreedEvents)} took {Time.FormatDuration(DateTime.UtcNow - start)}");
                    return;
                }
                GetContracts().WaitUntilNextPeriod();
            }
            Assert.Fail($"{nameof(WaitForSlotFreedEvents)} failed after {Time.FormatDuration(timeout)}");
        }

        private TimeSpan CalculateContractFailTimespan()
        {
            var config = GetContracts().Deployment.Config;
            var maxSlashesBeforeSlotFreed = Convert.ToInt32(config.Collateral.MaxNumberOfSlashes);
            var numProofsMissedBeforeSlash = Convert.ToInt32(config.Collateral.SlashCriterion);

            var periodDuration = GetPeriodDuration();
            var requiredNumMissedProofs = maxSlashesBeforeSlotFreed * numProofsMissedBeforeSlash;

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
            var cid = client.UploadFile(GenerateTestFile(5.MB()));
            return client.Marketplace.RequestStorage(new StoragePurchaseRequest(cid)
            {
                Duration = TimeSpan.FromHours(1.0),
                Expiry = TimeSpan.FromHours(0.2),
                MinRequiredNumberOfNodes = (uint)NumberOfHosts,
                NodeFailureTolerance = (uint)(NumberOfHosts / 2),
                PricePerSlotPerSecond = pricePerSlotPerSecond,
                ProofProbability = 1, // Require a proof every period
                RequiredCollateral = 1.Tst()
            });
        }
    }
}
