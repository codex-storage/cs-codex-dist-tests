using CodexPlugin;
using NUnit.Framework;
using Utils;

namespace CodexTests.BasicTests
{
    [TestFixture]
    public class BlockExchangeTests : CodexDistTest
    {
        [Test]
        public void EmptyAfterExchange()
        {
            var bootstrap = AddCodex(s => s.WithName("bootstrap"));
            var node = AddCodex(s => s.WithName("node").WithBootstrapNode(bootstrap));

            AssertExchangeIsEmpty(bootstrap, node);

            var file = GenerateTestFile(1.MB());
            var cid = bootstrap.UploadFile(file);
            node.DownloadContent(cid);

            AssertExchangeIsEmpty(bootstrap, node);
        }

        [Test]
        public void EmptyAfterExchangeWithBystander()
        {
            var bootstrap = AddCodex(s => s.WithName("bootstrap"));
            var node = AddCodex(s => s.WithName("node").WithBootstrapNode(bootstrap));
            var bystander = AddCodex(s => s.WithName("bystander").WithBootstrapNode(bootstrap));

            AssertExchangeIsEmpty(bootstrap, node, bystander);

            var file = GenerateTestFile(1.MB());
            var cid = bootstrap.UploadFile(file);
            node.DownloadContent(cid);

            AssertExchangeIsEmpty(bootstrap, node, bystander);
        }

        private void AssertExchangeIsEmpty(params ICodexNode[] nodes)
        {
            foreach (var node in nodes)
            {
                // API Call not available in master-line Codex image.
                //Time.Retry(() => AssertBlockExchangeIsEmpty(node), nameof(AssertExchangeIsEmpty));
            }
        }

        //private void AssertBlockExchangeIsEmpty(ICodexNode node)
        //{
        //    var msg = $"BlockExchange for {node.GetName()}: ";
        //    var response = node.GetDebugBlockExchange();
        //    foreach (var peer in response.peers)
        //    {
        //        var activeWants = peer.wants.Where(w => !w.cancel).ToArray();
        //        Assert.That(activeWants.Length, Is.EqualTo(0), msg + "thinks a peer has active wants.");
        //    }
        //    Assert.That(response.taskQueue, Is.EqualTo(0), msg + "has tasks in queue.");
        //    Assert.That(response.pendingBlocks, Is.EqualTo(0), msg + "has pending blocks.");
        //}
        public BlockExchangeTests(string deployId) : base(deployId)
        {
        }
    }
}
