using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.MembershipChangeTests
{
    [TestFixture]
    public class MixedMembershipChangeTests : DistTest
    {
        public void MixedMembershipChange(int numberOfNodes, int filesize, int numberOfDownloaders, int numberOfUploaders, int numberOfNodesToAdd = 0, int numberOfNodesToRemove = 0)
        {
            // Creating the node groups
            ICodexNodeGroup? toAdd = null;
            ICodexNodeGroup? toRemove = null;
            var group = SetupCodexNodes(numberOfNodes);
            if (numberOfNodesToAdd != 0)
                toAdd = SetupCodexNodes(numberOfNodesToAdd);
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


            var testfiles = new List<TestFile>();
            var contentIds = new List<Task<ContentId>>();

            // Start adding and dropping nodes

            // Start the upload for each node in the main group
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles.Add(GenerateTestFile(filesize.MB()));
                var n = i;
                contentIds.Add(Task.Run(() => { return host.UploadFile(testfiles[n]); }));
            }
            for (var i = 0; (toAdd != null && i < toAdd.Count()) || (toRemove != null && i < toRemove.Count()); i++)
            {
                Log($"Iteration {i}");
                if (toAdd != null && i < toAdd.Count())
                    Task.Run(() => { host.ConnectToPeer(toAdd[i]); });
                if (toRemove != null && i < toRemove.Count())
                    Task.Run(() => { toRemove[i].BringOffline(); });
            }

            // Wait for the upload to finish
            Task.WaitAll(contentIds.ToArray());

            // Download the files
            var downloads = new List<Task<TestFile?>>();
            for (int i = 0; i < group.Count(); i++)
            {
                var n = i;
                downloads.Add(Task.Run(() => { return group[n].DownloadContent(contentIds[n].Result); }));
            }

            // Wait for the download to finish
            Task.WaitAll(downloads.ToArray());

            // Assert that the files are intact
            for (int i = 0; i < group.Count(); i++)
            {
                testfiles[i].AssertIsEqual(downloads[i].Result);
            }
        }
    }
}