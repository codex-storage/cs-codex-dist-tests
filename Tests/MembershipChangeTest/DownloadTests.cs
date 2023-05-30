using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.MembershipChangeTests
{
    [TestFixture]
    public class DownloadMembershipChangeTests : DistTest
    {
        [TestCase(1, 100, 5, 0)]
        [TestCase(1, 100, 0, 5)]
        [TestCase(1, 100, 5, 5)]
        [UseLongTimeouts]
        public void DownloadMembershipChange(int numberOfNodes, int filesize, int numberOfNodesToAdd = 0, int numberOfNodesToRemove = 0)
        {
            // Setup 3 node groups, one which will be added during the procedure, one which will be dropped during the procedure, and the one being tested. 
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

            // Upload a file to the host
            var testFile = GenerateTestFile(filesize.MB());
            var contentId = host.UploadFile(testFile);

            var list = new List<Task<TestFile?>>();
                        
            // Start the download for each node
            foreach (var node in group)
            {
                list.Add(Task.Run(() => { return node.DownloadContent(contentId); }));
            }

            // Start adding and dropping nodes during the download
            // TODO: The log is put here for debug, but without it the members do not run async
            for (var i = 0; (toAdd != null && i < toAdd.Count()) || (toRemove != null && i < toRemove.Count()); i++)
            {
                Log($"Iteration {i}");
                if (toAdd != null && i < toAdd.Count())
                    Task.Run(() => { host.ConnectToPeer(toAdd[i]); });
                if (toRemove != null && i < toRemove.Count())
                    Task.Run(() => { toRemove[i].BringOffline(); });
            }

            // Wait for the download to finish
            Task.WaitAll(list.ToArray());

            // Assert that the download was successful
            foreach (var task in list)
            {
                testFile.AssertIsEqual(task.Result);
            }
        }
    }
}