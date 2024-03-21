using Logging;
using Moq;
using NethereumWorkflow;
using NUnit.Framework;

namespace FrameworkTests.NethereumWorkflow
{
    [TestFixture]
    public class BlockTimeFinderTests
    {
        private Mock<IWeb3Blocks> web3 = new Mock<IWeb3Blocks>();
        private Mock<ILog> log = new Mock<ILog>();
        private Dictionary<decimal, Block> blocks = new Dictionary<decimal, Block>();
        private DateTime start = DateTime.Now;

        private BlockTimeFinder finder = null!;

        private void SetupContinuousBlockchain()
        {
            start = DateTime.UtcNow.AddDays(-1).AddSeconds(-30);
            blocks = new Dictionary<decimal, Block>();
            
            for (var i = 0; i < 30; i++)
            {
                decimal d = 100 + i;
                blocks.Add(d, new Block(d, start + TimeSpan.FromSeconds(i * 2)));
            }
        }

        [SetUp]
        public void SetUp()
        {
            SetupContinuousBlockchain();

            web3 = new Mock<IWeb3Blocks>();
            web3.Setup(w => w.GetCurrentBlockNumber()).Returns(blocks.Keys.Max());
            web3.Setup(w => w.GetTimestampForBlock(It.IsAny<decimal>())).Returns<decimal>(d =>
            {
                if (blocks.ContainsKey(d)) return blocks[d].Time;
                return null;
            });

            finder = new BlockTimeFinder(web3.Object, log.Object);
        }

        [Test]
        public void FindsMiddleOfChain()
        {
            var b1 = blocks[115];
            var b2 = blocks[116];

            var momentBetween = b1.JustAfter;

            var b1Number = finder.GetHighestBlockNumberBefore(momentBetween);
            var b2Number = finder.GetLowestBlockNumberAfter(momentBetween);

            Assert.That(b1Number, Is.EqualTo(b1.Number));
            Assert.That(b2Number, Is.EqualTo(b2.Number));
        }

        [Test]
        public void FindsFrontOfChain_Lowest()
        {
            var first = blocks.First().Value;

            var firstNumber = finder.GetLowestBlockNumberAfter(first.JustBefore);

            Assert.That(firstNumber, Is.EqualTo(first.Number));
        }

        [Test]
        public void FindsFrontOfChain_Highest()
        {
            var first = blocks.First().Value;

            var firstNumber = finder.GetHighestBlockNumberBefore(first.JustAfter);

            Assert.That(firstNumber, Is.EqualTo(first.Number));
        }

        [Test]
        public void FindsTailOfChain_Lowest()
        {
            var last = blocks.Last().Value;

            var lastNumber = finder.GetLowestBlockNumberAfter(last.JustBefore);

            Assert.That(lastNumber, Is.EqualTo(last.Number));
        }

        [Test]
        public void FindsTailOfChain_Highest()
        {
            var last = blocks.Last().Value;

            var lastNumber = finder.GetHighestBlockNumberBefore(last.JustAfter);

            Assert.That(lastNumber, Is.EqualTo(last.Number));
        }

        [Test]
        public void FailsToFindBlockBeforeFrontOfChain()
        {
            var first = blocks.First().Value;

            var notFound = finder.GetHighestBlockNumberBefore(first.Time);

            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void FailsToFindBlockAfterTailOfChain()
        {
            var last = blocks.Last().Value;

            var notFound = finder.GetLowestBlockNumberAfter(last.Time);

            Assert.That(notFound, Is.Null);
        }

    }

    public class Block
    {
        public Block(decimal number, DateTime time)
        {
            Number = number;
            Time = time;
        }

        public decimal Number { get; }
        public DateTime Time { get; }
        public DateTime JustBefore { get { return Time.AddSeconds(-1); } }
        public DateTime JustAfter { get { return Time.AddSeconds(1); } }
    }

}
