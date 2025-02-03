using CodexClient;
using CodexTests;
using NUnit.Framework;
using Utils;

namespace ExperimentalTests.BasicTests
{
    [TestFixture]
    public class PyramidTests : CodexDistTest
    {
        [Test]
        [CreateTranscript(nameof(PyramidTest))]
        public void PyramidTest()
        {
            var size = 5.MB();
            var numberOfLayers = 3;

            var bottomLayer = StartLayers(numberOfLayers);

            var cids = UploadFiles(bottomLayer, size);

            DownloadAllFilesFromEachNodeInLayer(bottomLayer, cids);
        }

        private List<ICodexNode> StartLayers(int numberOfLayers)
        {
            var layer = new List<ICodexNode>();
            layer.Add(StartCodex(s => s.WithName("Top")));

            for (var i = 0; i < numberOfLayers; i++)
            {
                var newLayer = new List<ICodexNode>();
                foreach (var node in layer)
                {
                    newLayer.AddRange(StartCodex(2, s => s.WithBootstrapNode(node).WithName("Layer[" + i + "]")));
                }

                layer.Clear();
                layer.AddRange(newLayer);
            }

            return layer;
        }

        private ContentId[] UploadFiles(List<ICodexNode> layer, ByteSize size)
        {
            var uploadTasks = new List<Task<ContentId>>();
            foreach (var node in layer)
            {
                uploadTasks.Add(Task.Run(() =>
                {
                    var file = GenerateTestFile(size);
                    return node.UploadFile(file);
                }));
            }

            var cids = uploadTasks.Select(t =>
            {
                t.Wait();
                return t.Result;
            }).ToArray();

            return cids;
        }

        private void DownloadAllFilesFromEachNodeInLayer(List<ICodexNode> layer, ContentId[] cids)
        {
            var downloadTasks = new List<Task>();
            foreach (var node in layer)
            {
                downloadTasks.Add(Task.Run(() =>
                {
                    var dlCids = RandomUtils.Shuffled(cids);
                    foreach (var cid in dlCids)
                    {
                        node.DownloadContent(cid);
                    }
                }));
            }

            Task.WaitAll(downloadTasks.ToArray());
        }
    }
}
