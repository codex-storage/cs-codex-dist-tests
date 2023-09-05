using DistTestCore.Helpers;

namespace ContinuousTests.Tests
{
    public class FetchTest : ContinuousTest
    {
        public override int RequiredNumberOfNodes => -1;
        public override TimeSpan RunTestEvery => TimeSpan.FromMinutes(2);
        public override TestFailMode TestFailMode => TestFailMode.AlwaysRunAllMoments;

        [TestMoment(t: 0)]
        public void CheckConnectivity()
        {
            var checker = new PeerFetchTestHelpers(Log, FileManager);
            checker.AssertFullFetchInterconnectivity(Nodes);
        }
    }
}
