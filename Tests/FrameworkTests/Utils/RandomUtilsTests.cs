using NUnit.Framework;
using Utils;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class RandomUtilsTests
    {
        [Test]
        [Combinatorial]
        public void TestRandomStringLength(
            [Values(1, 5, 10, 1023, 1024, 1025, 2222)] int length)
        {
            var str = RandomUtils.GenerateRandomString(length);

            Assert.That(str.Length, Is.EqualTo(length));
        }
    }
}
