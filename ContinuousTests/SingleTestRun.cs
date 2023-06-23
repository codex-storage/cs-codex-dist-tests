using DistTestCore.Codex;
using DistTestCore;
using Logging;
using Utils;
using KubernetesWorkflow;

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
            var containers = SelectRandomContainers(number);
            testLog.Log("Selected nodes: " + string.Join(",", containers.Select(c => c.Name)));
            return codexNodeFactory.Create(containers, testLog, test.TimeSet);
        }

        private RunningContainer[] SelectRandomContainers(int number)
        {
            var containers = config.CodexDeployment.CodexContainers.ToList();
            var result = new RunningContainer[number];
            for (var i = 0; i < number; i++)
            {
                result[i] = containers.PickOneRandom();
            }
            return result;
        }
    }
}
