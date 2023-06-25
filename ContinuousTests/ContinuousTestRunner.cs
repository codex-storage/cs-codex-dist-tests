namespace ContinuousTests
{
    public class ContinuousTestRunner
    {
        private readonly ConfigLoader configLoader = new ConfigLoader();
        private readonly TestFactory testFactory = new TestFactory();
        private readonly Configuration config;
        private readonly StartupChecker startupChecker;

        public ContinuousTestRunner()
        {
            config = configLoader.Load();
            startupChecker = new StartupChecker(config);
        }

        public void Run()
        {
            startupChecker.Check();

            var allTests = testFactory.CreateTests();
            var testStarters = allTests.Select(t => new TestStarter(config, t.GetType(), t.RunTestEvery)).ToArray();

            foreach (var t in testStarters)
            {
                t.Begin();
            }
          
            Thread.Sleep(TimeSpan.MaxValue);
        }
    }
}
