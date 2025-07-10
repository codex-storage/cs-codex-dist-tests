using NUnit.Framework;
using Utils;

namespace CodexReleaseTests.Utils
{
    public class PurchaseParams
    {
        private readonly ByteSize blockSize = 64.KB();

        public PurchaseParams(int nodes, int tolerance, ByteSize uploadFilesize)
        {
            Nodes = nodes;
            Tolerance = tolerance;
            UploadFilesize = uploadFilesize;

            EncodedDatasetSize = CalculateEncodedDatasetSize();
            SlotSize = CalculateSlotSize();

            Assert.That(IsPowerOfTwo(SlotSize));
        }

        public int Nodes { get; }
        public int Tolerance { get; }
        public ByteSize UploadFilesize { get; }
        public ByteSize EncodedDatasetSize { get; }
        public ByteSize SlotSize { get; }

        private ByteSize CalculateSlotSize()
        {
            // encoded dataset is divided over the nodes.
            // then each slot is rounded up to the nearest power-of-two blocks.
            var numBlocks = EncodedDatasetSize.DivUp(blockSize);
            var numSlotBlocks = 1 + ((numBlocks - 1) / Nodes); // round-up div.

            // Next power of two:
            var numSlotBlocksPow2 = IsOrNextPowerOf2(numSlotBlocks);
            return new ByteSize(blockSize.SizeInBytes * numSlotBlocksPow2);
        }

        private ByteSize CalculateEncodedDatasetSize()
        {
            var numBlocks = UploadFilesize.DivUp(blockSize);

            var ecK = Nodes - Tolerance;
            var ecM = Tolerance;

            // for each K blocks, we generate M parity blocks
            var numParityBlocks = (numBlocks / ecK) * ecM;
            var totalBlocks = numBlocks + numParityBlocks;

            return new ByteSize(blockSize.SizeInBytes * totalBlocks);
        }

        private int IsOrNextPowerOf2(int n)
        {
            if (IsPowerOfTwo(n)) return n;
            n = n - 1;
            var lg = Convert.ToInt32(Math.Round(Math.Log2(Convert.ToDouble(n))));
            return 1 << (lg + 1);
        }

        private static bool IsPowerOfTwo(ByteSize size)
        {
            var x = size.SizeInBytes;
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        private static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }
    }
}
