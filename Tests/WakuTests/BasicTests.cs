using NUnit.Framework;
using WakuPlugin;

namespace WakuTests
{
    public class BasicTests : WakuDistTest
    {
        [Test]
        public void Hi()
        {
            var rc = Ci.DeployWakuNodes(1);

            var i = 0;
        }
    }
}