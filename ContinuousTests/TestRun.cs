using DistTestCore;
using DistTestCore.Codex;
using Logging;

namespace ContinuousTests
{
    public class TestRun
    {
        private readonly Random random = new Random();
        private readonly Configuration config;
        private readonly BaseLog log;
        private readonly TestFinder testFinder;
        private readonly CodexNode[] nodes;
        private readonly FileManager fileManager;

        public TestRun(Configuration config, BaseLog log, TestFinder testFinder, CodexNode[] nodes)
        {
            this.config = config;
            this.log = log;
            this.testFinder = testFinder;
            this.nodes = nodes;
            fileManager = new FileManager(log, new DistTestCore.Configuration());
        }

        public void Run()
        {
            var remainingTests = testFinder.GetTests().ToList();
            while (remainingTests.Any())
            {
                var test = PickOneRandom(remainingTests);
                var selectedNodes = SelectRandomNodes(test.RequiredNumberOfNodes);
                AssignEssentials(test, selectedNodes);
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

        private void AssignEssentials(IContinuousTest test, CodexNode[] nodes)
        {
            var t = (ContinuousTest)test;
            t.Nodes = nodes;
            t.Log = log;
            t.FileManager = fileManager;
        }

        private void ClearEssentials(IContinuousTest test)
        {
            var t = (ContinuousTest)test;
            t.Nodes = null!;
            t.Log = null!;
            t.FileManager = null!;
        }

        private CodexNode[] SelectRandomNodes(int number)
        {
            var remainingNodes = nodes.ToList();
            var result = new CodexNode[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = PickOneRandom(remainingNodes);
            }
            return result;
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
