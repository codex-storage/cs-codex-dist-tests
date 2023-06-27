namespace ContinuousTests
{
    public class TestMomentAttribute : Attribute
    {
        public TestMomentAttribute(int t)
        {
            T = t;
        }

        public int T { get; }
    }
}
