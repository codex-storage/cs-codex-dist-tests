using NUnit.Framework;
using System.Text;
using TestNetRewarder;

namespace FrameworkTests.Utils
{
    [TestFixture]
    public class EmojiMapsTests
    {
        private readonly Random random = new Random();
        private readonly EmojiMaps maps = new EmojiMaps();

        [Test]
        public void GeneratesConsistentStrings(
            [Values(1, 5, 10, 20)] int inputLength,
            [Values(1, 2, 3, 5)] int outLength)
        {
            var buffer = new byte[inputLength];
            random.NextBytes(buffer);
            var input = Encoding.ASCII.GetString(buffer);

            var out1 = maps.StringToEmojis(input, outLength);
            var out2 = maps.StringToEmojis(input, outLength);
            
            Assert.That(out1, Is.EqualTo(out2));
        }
    }
}
