using GethPlugin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class EthAddressEqualityTests
    {
        [Test]
        [Combinatorial]
        public void Equal(
            [Values(1, 2, 3, 4, 5)] int runs
        )
        {
            var account = EthAccountGenerator.GenerateNew();

            var str = account.EthAddress.Address;

            var addr = new EthAddress(str);

            Assert.That(addr, Is.EqualTo(account.EthAddress));
            Assert.That(addr == account.EthAddress);
            Assert.That(!(addr != account.EthAddress));
        }

        [Test]
        [Combinatorial]
        public void NotEqual(
            [Values(1, 2, 3, 4, 5)] int runs
        )
        {
            var account1 = EthAccountGenerator.GenerateNew();
            var account2 = EthAccountGenerator.GenerateNew();

            Assert.That(account1.EthAddress, Is.Not.EqualTo(account2.EthAddress));
            Assert.That(account1.EthAddress != account2.EthAddress);
            Assert.That(!(account1.EthAddress == account2.EthAddress));
        }
    }
}
