using Core;
using DistTestCore;

namespace WakuTests
{
    public class WakuDistTest : DistTest
    {
        public WakuDistTest()
        {
            ProjectPlugin.Load<WakuPlugin.WakuPlugin>();
        }
    }
}
