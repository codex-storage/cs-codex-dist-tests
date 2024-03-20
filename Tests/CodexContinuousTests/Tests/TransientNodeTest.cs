using CodexPlugin;
using FileUtils;
using NUnit.Framework;
using Utils;

namespace ContinuousTests.Tests
{
    public class TransientNodeTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => 3;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(1);
        public override TestFailMode TestFailMode => TestFailMode.StopAfterFirstFailure;
        public override string CustomK8sNamespace => nameof(TransientNodeTest).ToLowerInvariant();

        private TrackedFile file = null!;
        private ContentId cid = null!;

        private ICodexNode UploadBootstapNode { get { return Nodes[0]; } }
        private ICodexNode DownloadBootstapNode { get { return Nodes[1]; } }
        private ICodexNode IntermediateNode { get { return Nodes[2]; } }

        [TestMoment(t: 0)]
        public void UploadWithTransientNode()
        {
            file = FileManager.GenerateFile(10.MB());

            NodeRunner.RunNode(UploadBootstapNode, 
                s => s.WithName("TransientUploader"),
                node =>
            {
                cid = node.UploadFile(file);
                Assert.That(cid, Is.Not.Null);

                var resultFile = IntermediateNode.DownloadContent(cid);
                file.AssertIsEqual(resultFile);
            });
        }

        [TestMoment(t: MinuteOne)]
        public void DownloadWithTransientNode()
        {
            NodeRunner.RunNode(DownloadBootstapNode,
                s => s.WithName("TransientDownloader"),
                node =>
            {
                var resultFile = node.DownloadContent(cid);
                file.AssertIsEqual(resultFile);
            });
        }
    }
}
