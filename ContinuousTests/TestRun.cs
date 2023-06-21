using DistTestCore;
using DistTestCore.Codex;
using Logging;

namespace ContinuousTests
{
    public class TestRun
    {
        private readonly Random random = new Random();
        private readonly CodexNodeFactory codexNodeFactory = new CodexNodeFactory();
        private readonly Configuration config;
        private readonly BaseLog log;
        private readonly TestFactory testFinder;
        private readonly FileManager fileManager;
        private ITimeSet timeSet;

        public TestRun(Configuration config, BaseLog log, TestFactory testFinder)
        {
            this.config = config;
            this.log = log;
            this.testFinder = testFinder;
            fileManager = new FileManager(log, new DistTestCore.Configuration());
            timeSet = new DefaultTimeSet();
        }

        public void Run()
        {
            var remainingTests = testFinder.CreateTests().ToList();
            while (remainingTests.Any())
            {
                var test = PickOneRandom(remainingTests);
                var nodes = CreateRandomNodes(test.RequiredNumberOfNodes);
                AssignEssentials(test, nodes);
                fileManager.PushFileSet();

                log.Log($"Start '{test.Name}'");
                try
                {
                    test.Run();
                    log.Log($"'{test.Name}' = Passed");
                }
                catch
                {
                    log.Log($"'{test.Name}' = Failed");
                }
                
                fileManager.PopFileSet();
                ClearEssentials(test);
                Thread.Sleep(config.SleepSecondsPerTest * 1000);
            }
        }

        private void AssignEssentials(ContinuousTest test, CodexNode[] nodes)
        {
            test.Initialize(nodes, log, fileManager);
        }

        private void ClearEssentials(ContinuousTest test)
        {
            // Looks a little strange, but prevents finished test from interacting further.
            test.Initialize(null!, null!, null!);
        }

        private string[] SelectRandomUrls(int number)
        {
            var urls = config.CodexUrls.ToList();
            var result = new string[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = PickOneRandom(urls);
            }
            return result;
        }

        private CodexNode[] CreateRandomNodes(int number)
        {
            var urls = SelectRandomUrls(number);
            return codexNodeFactory.Create(urls, log, timeSet);
        }

        private T PickOneRandom<T>(List<T> remainingItems)
        {
            var i = random.Next(0, remainingItems.Count);
            var result = remainingItems[i];
            remainingItems.RemoveAt(i);
            return result;
        }
    }
}
