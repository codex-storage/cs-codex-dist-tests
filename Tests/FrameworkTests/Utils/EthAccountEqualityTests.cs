using GethPlugin;
using NUnit.Framework;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class EthAccountEqualityTests
    {
        [Test]
        public void Accounts()
        {
            var account1 = EthAccountGenerator.GenerateNew();
            var account2 = EthAccountGenerator.GenerateNew();

            Assert.That(account1, Is.EqualTo(account1));
            Assert.That(account1 == account1);
            Assert.That(account1 != account2);
        }

        [Test]
        public void Addresses()
        {
            var address1 = EthAccountGenerator.GenerateNew().EthAddress;
            var address2 = EthAccountGenerator.GenerateNew().EthAddress;

            Assert.That(address1, Is.EqualTo(address1));
            Assert.That(address1 == address1);
            Assert.That(address1 != address2);
        }
    }
}
