using DistTestCore;
using KubernetesWorkflow;
using NUnit.Framework;
namespace Tests.MembershipChangeTests
{
    [TestFixture]
    public class MembershipChangeTests : DistTest
    {

        [Test]
        public void SingleDownloadWhileAdding()
        {
            var filesize = 100.MB();

            var group = SetupCodexNodes(1);
            var toAdd = SetupCodexNodes(5);
            var host = SetupCodexNodes(1)[0];

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesize);
            var contentId = host.UploadFile(testFile);
            var list = new List<Task>();
            foreach (var node in toAdd)
            {
                list.Add(Task.Run(() => { host.ConnectToPeer(node); }));
            }

            var resFile = group[0].DownloadContent(contentId);
            Task.WaitAll(list.ToArray());
            testFile.AssertIsEqual(resFile);
        }
        [Test]
        public void SingleUploadWhileAdding()
        {
            var filesize = 100.MB();

            var group = SetupCodexNodes(1);
            var toAdd = SetupCodexNodes(5);
            var host = SetupCodexNodes(1)[0];

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesize);
            var list = new List<Task>();
            foreach (var node in toAdd)
            {
                list.Add(Task.Run(() => { host.ConnectToPeer(node); }));
            }
            var contentId = host.UploadFile(testFile);


            Task.WaitAll(list.ToArray());
            var resFile = group[0].DownloadContent(contentId);
            testFile.AssertIsEqual(resFile);
        }
        [Test]
        public void SingleDownloadMixedMembership()
        {
            var filesize = 100.MB();

            var group = SetupCodexNodes(1);
            var toAdd = SetupCodexNodes(5);
            var toRemove = SetupCodexNodes(5);
            var host = SetupCodexNodes(1)[0];

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }
            foreach (var node in toRemove)
            {
                host.ConnectToPeer(node);
            }


            var testFile = GenerateTestFile(filesize);
            var contentId = host.UploadFile(testFile);

            for (var i = 0; i < toAdd.Count(); i++)
            {
                Task.Run(() => { host.ConnectToPeer(toAdd[i]); });
                Task.Run(() => { toRemove[i].BringOffline(); });
            }

            var resFile = group[0].DownloadContent(contentId);
            testFile.AssertIsEqual(resFile);
        }
        [Test]
        public void SingleUploadMixedMembership()
        {
            var filesize = 100.MB();

            var group = SetupCodexNodes(1);
            var toAdd = SetupCodexNodes(5);
            var toRemove = SetupCodexNodes(5);
            var host = SetupCodexNodes(1)[0];

            foreach (var node in group)
            {
                host.ConnectToPeer(node);
            }
            foreach (var node in toRemove)
            {
                host.ConnectToPeer(node);
            }

            var testFile = GenerateTestFile(filesize);

            var list = new List<Task>();

            for (var i = 0; i < toAdd.Count(); i++)
            {
                Task.Run(() => { host.ConnectToPeer(toAdd[i]); });
                Task.Run(() => { toRemove[i].BringOffline(); });
            }
            var contentId = host.UploadFile(testFile);

            var resFile = group[0].DownloadContent(contentId);
            testFile.AssertIsEqual(resFile);
        }
    }

}