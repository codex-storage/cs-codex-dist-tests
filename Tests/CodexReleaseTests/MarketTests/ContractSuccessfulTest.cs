using CodexContractsPlugin;
using CodexPlugin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class ContractSuccessfulTest : MarketplaceAutoBootstrapDistTest
    {
        private const int NumberOfHosts = 4;
        private const int FilesizeMb = 10;

        [Test]
        public void ContractSuccessful()
        {
            var hosts = StartHosts();

            var client = StartCodex(s => s.WithName("client"));

        }

        private ICodexNodeGroup StartHosts()
        {
            var hosts = StartCodex(NumberOfHosts, s => s.WithName("host"));

            var config = GetContracts().Deployment.Config;
            foreach (var host in hosts)
            {
                host.Marketplace.MakeStorageAvailable(new CodexPlugin.StorageAvailability(
                    totalSpace: (5 * FilesizeMb).MB(),
                    maxDuration: TimeSpan.FromSeconds(((double)config.Proofs.Period) * 5.0),
                    minPriceForTotalSpace: 1.TstWei(),
                    maxCollateral: 999999.Tst())
                );
            }
            return hosts;
        }
    }
}
