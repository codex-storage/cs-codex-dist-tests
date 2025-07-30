using BlockchainUtils;
using CodexContractsPlugin;
using CodexPlugin;
using DistTestCore;
using GethPlugin;
using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.MarketTests
{
    [TestFixture]
    public class TestTokenTransferTest : DistTest
    {
        private readonly EthAccount user1 = EthAccountGenerator.GenerateNew();
        private readonly EthAccount user2 = EthAccountGenerator.GenerateNew();

        [Test]
        public void CanTransferTokens()
        {
            var node = Ci.StartCodexNode();
            var blockCache = new BlockCache();
            var geth = Ci.StartGethNode(blockCache, s => s.IsMiner());
            var contracts = Ci.StartCodexContracts(geth, node.Version);

            geth.SendEth(user1.EthAddress, 1.Eth());
            geth.SendEth(user2.EthAddress, 1.Eth());

            contracts.MintTestTokens(user1.EthAddress, 10.Tst());
            Balances(contracts, 10.Tst(), 0.Tst());

            var geth1 = geth.WithDifferentAccount(user1);
            var geth2 = geth.WithDifferentAccount(user2);
            var contracts1 = contracts.WithDifferentGeth(geth1);
            var contracts2 = contracts.WithDifferentGeth(geth2);

            contracts1.TransferTestTokens(user2.EthAddress, 5.Tst());
            Balances(contracts, 5.Tst(), 5.Tst());

            contracts2.TransferTestTokens(user1.EthAddress, 2.Tst());
            Balances(contracts, 7.Tst(), 3.Tst());
        }

        private void Balances(ICodexContracts contracts, TestToken one, TestToken two)
        {
            var balance1 = contracts.GetTestTokenBalance(user1.EthAddress);
            var balance2 = contracts.GetTestTokenBalance(user2.EthAddress);
            Assert.That(balance1, Is.EqualTo(one));
            Assert.That(balance2, Is.EqualTo(two));
        }
    }
}
