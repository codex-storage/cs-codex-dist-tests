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
        public void CanTransferTestTokens()
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

            contracts1.TransferTestTokens(user2.EthAddress, (0.5m).Tst());
            Balances(contracts, (9.5m).Tst(), (0.5m).Tst());

            contracts2.TransferTestTokens(user1.EthAddress, (0.2m).Tst());
            Balances(contracts, (9.7m).Tst(), (0.3m).Tst());
        }

        [Test]
        public void CanTransferEth()
        {
            var blockCache = new BlockCache();
            var geth = Ci.StartGethNode(blockCache, s => s.IsMiner());

            geth.SendEth(user1.EthAddress, 1.Eth());
            geth.SendEth(user2.EthAddress, 1.Eth());

            Balances(geth, 1.Eth(), 1.Eth());

            var geth1 = geth.WithDifferentAccount(user1);
            var geth2 = geth.WithDifferentAccount(user2);

            geth1.SendEth(user2.EthAddress, (0.5m).Eth());
            Balances(geth, (0.5m).Eth(), (1.5m).Eth());

            geth2.SendEth(user1.EthAddress, (0.2m).Eth());
            Balances(geth, (0.7m).Eth(), (1.3m).Eth());
        }

        private void Balances(ICodexContracts contracts, TestToken one, TestToken two)
        {
            var balance1 = contracts.GetTestTokenBalance(user1.EthAddress);
            var balance2 = contracts.GetTestTokenBalance(user2.EthAddress);
            Assert.That(balance1, Is.EqualTo(one));
            Assert.That(balance2, Is.EqualTo(two));
        }

        private void Balances(IGethNode geth, Ether one, Ether two)
        {
            var balance1 = geth.GetEthBalance(user1.EthAddress);
            var balance2 = geth.GetEthBalance(user2.EthAddress);

            InRange(balance1, one);
            InRange(balance2, two);
        }

        private void InRange(Ether balance, Ether expected)
        {
            var gasTolerance = (0.001m).Eth();
            var max = expected + gasTolerance;
            var min = expected - gasTolerance;

            Assert.That(balance, Is.LessThanOrEqualTo(max).And.GreaterThanOrEqualTo(min));
        }
    }
}
