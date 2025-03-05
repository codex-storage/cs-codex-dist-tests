using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class EtherEqualityTests
    {
        [Test]
        [Combinatorial]
        public void Equal(
            [Values(1, 22, 333, 4444, 55555)] int amount,
            [Values(true, false)] bool isWei
        )
        {
            var amount1 = CreateEth(amount, isWei);
            var amount2 = CreateEth(amount, isWei);

            Assert.That(amount1, Is.EqualTo(amount2));
            Assert.That(amount1 == amount2);
            Assert.That(!(amount1 != amount2));
        }

        [Test]
        [Combinatorial]
        public void NotEqual(
            [Values(22, 333, 4444, 55555)] int amount,
            [Values(true, false)] bool isWei,
            [Values(1, 2, 10, -1, -2, -10)] int deltaWei
        )
        {
            var amount1 = CreateEth(amount, isWei);
            var amount2 = CreateEth(amount, isWei) + deltaWei.Wei();

            Assert.That(amount1, Is.Not.EqualTo(amount2));
            Assert.That(amount1 != amount2);
            Assert.That(!(amount1 == amount2));
        }

        private Ether CreateEth(int amount, bool isWei)
        {
            if (isWei) return amount.Wei();
            return amount.Eth();
        }
    }
}