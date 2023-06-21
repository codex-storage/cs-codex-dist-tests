namespace ContinuousTests
{
    public class TestFinder
    {
        private readonly List<IContinuousTest> testList = new List<IContinuousTest>();

        public IContinuousTest[] GetTests()
        {
            if (!testList.Any()) FindTests();
            return testList.ToArray();
        }

        private void FindTests()
        {
            var types = GetType().Assembly.GetTypes();
            var testTypes = types.Where(t => typeof(IContinuousTest).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (var testType in testTypes)
            {
                var t = Activator.CreateInstance(testType);
                testList.Add((IContinuousTest)t!);
            }
        }
    }
}
