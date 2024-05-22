using CodexContractsPlugin;
using NUnit.Framework;
using System.Numerics;

namespace FrameworkTests.CodexContractsPlugin
{
    [TestFixture]
    public class TestTokenTests
    {
        private const decimal factor = 1000000000000000000m;

        [Test]
        public void RepresentsSmallAmount()
        {
            var t = 10.TstWei();

            Assert.That(t.TstWei, Is.EqualTo(new BigInteger(10)));
            Assert.That(t.Tst, Is.EqualTo(new BigInteger(0)));
            Assert.That(t.ToString(), Is.EqualTo("10 TSTWEI"));
        }

        [Test]
        public void RepresentsLargeAmount()
        {
            var t = 10.Tst();

            var expected = new BigInteger(10 * factor);
            Assert.That(t.TstWei, Is.EqualTo(expected));
            Assert.That(t.Tst, Is.EqualTo(new BigInteger(10)));
            Assert.That(t.ToString(), Is.EqualTo("10 TST"));
        }

        [Test]
        public void RepresentsLongAmount()
        {
            var a = 10.Tst();
            var b = 20.TstWei();
            var t = a + b;

            var expected = new BigInteger((10 * factor) + 20);
            Assert.That(t.TstWei, Is.EqualTo(expected));
            Assert.That(t.Tst, Is.EqualTo(new BigInteger(10)));
            Assert.That(t.ToString(), Is.EqualTo("10 TST + 20 TSTWEI"));
        }
    }
}
