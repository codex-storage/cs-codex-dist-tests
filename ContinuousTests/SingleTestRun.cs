using DistTestCore.Codex;
using DistTestCore;
using Logging;
using Utils;

namespace ContinuousTests
{
    public class SingleTestRun
    {
        private readonly CodexNodeFactory codexNodeFactory = new CodexNodeFactory();
        private readonly Configuration config;
        private readonly ContinuousTest test;
        private readonly CodexNode[] nodes;
        private readonly FileManager fileManager;

        public SingleTestRun(Configuration config, ContinuousTest test, BaseLog testLog)
        {
            this.config = config;
            this.test = test;

            nodes = CreateRandomNodes(test.RequiredNumberOfNodes, testLog);
            fileManager = new FileManager(testLog, new DistTestCore.Configuration());

            test.Initialize(nodes, testLog, fileManager);
        }

        public void Run()
        {
            test.Run();
        }

        public void TearDown()
        {
            test.Initialize(null!, null!, null!);
            fileManager.DeleteAllTestFiles();
        }

        private CodexNode[] CreateRandomNodes(int number, BaseLog testLog)
        {
            var urls = SelectRandomUrls(number);
            testLog.Log("Selected nodes: " + string.Join(",", urls));
            return codexNodeFactory.Create(urls, testLog, test.TimeSet);
        }

        private string[] SelectRandomUrls(int number)
        {
            var urls = config.CodexUrls.ToList();
            var result = new string[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = urls.PickOneRandom();
            }
            return result;
        }
    }
}
