using DistTestCore;
using DistTestCore.Codex;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    public class TransientNodeTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 3;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(10);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
        public override string CustomK8sNamespace => nameof(TransientNodeTest).ToLowerInvariant();
        public override int EthereumAccountIndex => 201;

        private TestFile file = null!;
        private ContentId cid = null!;

        private CodexNode UploadBootstapNode { get { return Nodes[0]; } }
        private CodexNode DownloadBootstapNode { get { return Nodes[1]; } }
        private CodexNode IntermediateNode { get { return Nodes[2]; } }

        [TestMoment(t: 0)]
        public void UploadWithTransientNode()
        {
            file = FileManager.GenerateTestFile(10.MB());

            NodeRunner.RunNode(UploadBootstapNode, (codexAccess, marketplaceAccess) =>
            {
                cid = UploadFile(codexAccess.Node, file)!;
                Assert.That(cid, Is.Not.Null);

                var resultFile = DownloadFile(IntermediateNode, cid);
                file.AssertIsEqual(resultFile);
            });
        }

        [TestMoment(t: MinuteFive)]
        public void DownloadWithTransientNode()
        {
            NodeRunner.RunNode(DownloadBootstapNode, (codexAccess, marketplaceAccess) =>
            {
                var resultFile = DownloadFile(codexAccess.Node, cid);
                file.AssertIsEqual(resultFile);
            });
        }
    }
}
