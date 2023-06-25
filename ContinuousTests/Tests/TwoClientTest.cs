using DistTestCore;
using NUnit.Framework;

namespace ContinuousTests.Tests
{
    //public class TwoClientTest : ContinuousTest
    //{
    //    public override int RequiredNumberOfNodes => 2;

    //    public override void Run()
    //    {
    //        var file = FileManager.GenerateTestFile(10.MB());

    //        var cid = UploadFile(Nodes[0], file);
    //        Assert.That(cid, Is.Not.Null);

    //        var dl = DownloadContent(Nodes[1], cid!);

    //        file.AssertIsEqual(dl);
    //    }
    //}
}
