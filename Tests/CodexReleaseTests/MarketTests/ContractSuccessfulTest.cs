using CodexContractsPlugin;
using CodexTests;
using GethPlugin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class ContractSuccessfulTest : AutoBootstrapDistTest
    {
        [Test]
        public void ContractSuccessful()
        {
            var geth = Ci.StartGethNode(s => s.IsMiner());
            var contracts = Ci.DeployCodexContracts(geth);

        }
    }
}
