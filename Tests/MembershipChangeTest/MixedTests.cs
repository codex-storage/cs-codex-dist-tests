using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.MembershipChangeTests
{
    [TestFixture]
    public class MixedMembershipChangeTests : DistTest
    {
        [TestCase(1, 5, 0, 0)]

        [UseLongTimeouts]
        public void MixedMembershipChange(int numberOfNodes, int filesize, int numberOfNodesToAdd, int numberOfNodesToRemove)
        {
            // Creating the node groups
            ICodexNodeGroup? toAdd = null;
            ICodexNodeGroup? toAddSecondary = null;
            ICodexNodeGroup? toRemove = null;
            var group = SetupCodexNodes(numberOfNodes);
            
            if (numberOfNodesToAdd != 0) {
                toAdd = SetupCodexNodes(numberOfNodesToAdd);
                toAddSecondary = SetupCodexNodes(numberOfNodesToAdd);
            }
            if (numberOfNodesToRemove != 0)
                toRemove = SetupCodexNodes(numberOfNodesToRemove);
            var host = SetupCodexNodes(1)[0];

            // Connect the main and dropping nodes to the host
            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }
            if (toRemove != null)
                foreach (var node in toRemove)
                    host.ConnectToPeer(node);

            var testFile = GenerateTestFile(filesize.MB());
            var contentId = host.UploadFile(testFile);

            var testfiles = new List<TestFile>();
            var contentIds = new List<Task<ContentId>>();

            var downloads = new List<Task<TestFile?>>();

            // Start adding and dropping nodes
            for (var i = 0; (toAdd != null && i < toAdd.Count()) || (toRemove != null && i < toRemove.Count()); i++)
            {
                Log($"Iteration {i}");
                if (toAdd != null && i < toAdd.Count())
                    Task.Run(() => { host.ConnectToPeer(toAdd[i]); });
                if (toRemove != null && i < toRemove.Count())
                    Task.Run(() => { toRemove[i].BringOffline(); });
            }

            // Start the upload for each node in the main group
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesize.MB()));
                var n = i;
                contentIds.Add(Task.Run(() => { return host.UploadFile(testfiles[n]); }));
            }

            // Start the download for each node in the main group
            for (int i = 0; i < group.Count(); i++)
            {
                var n = i;
                downloads.Add(Task.Run(() => { return group[n].DownloadContent(contentId); }));
            }

            // Wait for the download to finish
            Task.WaitAll(downloads.ToArray());

            // Wait for the upload to finish
            Task.WaitAll(contentIds.ToArray());

            // Assert that the files are intact
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles[i].AssertIsEqual(downloads[i].Result);
            }
        }
    }
}