namespace ContinuousTests
{
    public class TestFactory
    {
        public ContinuousTest[] CreateTests()
        {
            var types = GetType().Assembly.GetTypes();
            var testTypes = types.Where(t => typeof(ContinuousTest).IsAssignableFrom(t) && !t.IsAbstract);
            return testTypes.Select(t => (ContinuousTest)Activator.CreateInstance(t)!).ToArray();
        }
    }
}
