using NUnit.Framework;
using Utils;

namespace FrameworkTests.CodexContractsPlugin
{
    [TestFixture]
    public class TestTokenEqualityTests
    {
        [Test]
        [Combinatorial]
        public void Equal(
            [Values(1, 22, 333, 4444, 55555)] int amount,
            [Values(true, false)] bool isWei
        )
        {
            var amount1 = CreateTst(amount, isWei);
            var amount2 = CreateTst(amount, isWei);

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
            var amount1 = CreateTst(amount, isWei);
            var amount2 = CreateTst(amount, isWei) + deltaWei.TstWei();

            Assert.That(amount1, Is.Not.EqualTo(amount2));
            Assert.That(amount1 != amount2);
            Assert.That(!(amount1 == amount2));
        }

        private TestToken CreateTst(int amount, bool isWei)
        {
            if (isWei) return amount.TstWei();
            return amount.Tst();
        }
    }
}