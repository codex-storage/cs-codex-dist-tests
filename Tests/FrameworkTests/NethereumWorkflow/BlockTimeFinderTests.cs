using BlockchainUtils;
using Logging;
using Moq;
using NethereumWorkflow;
using NethereumWorkflow.BlockUtils;
using NUnit.Framework;

namespace FrameworkTests.NethereumWorkflow
{
    [TestFixture]
    public class BlockTimeFinderTests
    {
        private readonly Mock<ILog> log = new Mock<ILog>();
        private Mock<IWeb3Blocks> web3 = new Mock<IWeb3Blocks>();
        private Dictionary<ulong, Block> blocks = new Dictionary<ulong, Block>();

        private BlockTimeFinder finder = null!;

        private void SetupBlockchain()
        {
            var start = DateTime.UtcNow.AddDays(-1).AddSeconds(-30);
            blocks = new Dictionary<ulong, Block>();

            Block? prev = null;
            for (ulong i = 0; i < 30; i++)
            {
                ulong d = 100 + i;
                var newBlock = new Block(d, start + TimeSpan.FromSeconds(i * 2));
                blocks.Add(d, newBlock);

                if (prev != null)
                {
                    prev.Next = newBlock;
                    newBlock.Previous = prev;
                }
                prev = newBlock;
            }
        }

        [SetUp]
        public void SetUp()
        {
            SetupBlockchain();

            web3 = new Mock<IWeb3Blocks>();
            web3.Setup(w => w.GetCurrentBlockNumber()).Returns(blocks.Keys.Max());
            web3.Setup(w => w.GetTimestampForBlock(It.IsAny<ulong>())).Returns<ulong>(d =>
            {
                if (blocks.ContainsKey(d)) return blocks[d].Time;
                return null;
            });

            finder = new BlockTimeFinder(new BlockCache(), web3.Object, log.Object);
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
        public void FindsGenesisBlockAtFrontOfChain()
        {
            var first = blocks.First().Value;

            var firstNumber = finder.GetHighestBlockNumberBefore(first.Time);

            Assert.That(firstNumber, Is.EqualTo(first.Number));
        }

        [Test]
        public void FindsCurrentBlockAtTailOfChain()
        {
            var last = blocks.Last().Value;

            var lastNumber = finder.GetLowestBlockNumberAfter(last.Time);

            Assert.That(lastNumber, Is.EqualTo(last.Number));
        }

        [Test]
        public void FailsToFindBlockBeforeFrontOfChain_history()
        {
            var first = blocks.First().Value;

            var notFound = finder.GetHighestBlockNumberBefore(first.JustBefore);

            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void FailsToFindBlockAfterTailOfChain_future()
        {
            var last = blocks.Last().Value;

            var notFound = finder.GetLowestBlockNumberAfter(last.JustAfter);

            Assert.That(notFound, Is.Null);
        }

        [Test]
        public void RunThrough()
        {
            foreach (var pair in blocks)
            {
                var block = pair.Value;

                AssertLink(block.Previous, finder.GetHighestBlockNumberBefore(block.JustBefore));
                AssertLink(block, finder.GetHighestBlockNumberBefore(block.Time));
                AssertLink(block, finder.GetHighestBlockNumberBefore(block.JustAfter));

                AssertLink(block, finder.GetLowestBlockNumberAfter(block.JustBefore));
                AssertLink(block, finder.GetLowestBlockNumberAfter(block.Time));
                AssertLink(block.Next, finder.GetLowestBlockNumberAfter(block.JustAfter));
            }
        }

        private void AssertLink(Block? expected, ulong? actual)
        {
            if (expected == null)
            {
                Assert.That(actual, Is.Null);
            }
            else
            {
                Assert.That(expected.Number, Is.EqualTo(actual!.Value));
            }
        }
    }

    public class Block
    {
        public Block(ulong number, DateTime time)
        {
            Number = number;
            Time = time;
        }

        public ulong Number { get; }
        public DateTime Time { get; }
        public DateTime JustBefore { get { return Time.AddSeconds(-1); } }
        public DateTime JustAfter { get { return Time.AddSeconds(1); } }

        public Block? Next { get; set; }
        public Block? Previous { get; set; }

        public override string ToString()
        {
            return $"[{Number}]";
        }
    }
}
