using CodexClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkTests.CodexClient
{
    [TestFixture]
    public class ContentIdTests
    {
        [Test]
        public void EqualityTest()
        {
            var a1 = new ContentId("a");
            var a2 = new ContentId("a");
            var b1 = new ContentId("b");

            Assert.That(a1, Is.EqualTo(a2));
            Assert.That(a1, Is.Not.EqualTo(b1));
            Assert.That(b1, Is.Not.EqualTo(a2));

            Assert.That(a1 == a2);
            Assert.That(a1 != b1);
            Assert.That(b1 != a2);
        }
    }
}
