using NUnit.Framework;
using Logging;
using Utils;
using Core;
using CodexPlugin;
using CodexClient;

namespace ContinuousTests
{
    public class NodeRunner
    {
        private readonly EntryPointFactory entryPointFactory = new EntryPointFactory();
        private readonly ICodexNode[] nodes;
        private readonly Configuration config;
        private readonly ILog log;
        private readonly string customNamespace;

        public NodeRunner(ICodexNode[] nodes, Configuration config, ILog log, string customNamespace)
        {
            this.nodes = nodes;
            this.config = config;
            this.log = log;
            this.customNamespace = customNamespace;
        }

        public void RunNode(Action<ICodexSetup> setup, Action<ICodexNode> operation)
        {
            RunNode(nodes.ToList().PickOneRandom(), setup, operation);
        }

        public void RunNode(ICodexNode bootstrapNode, Action<ICodexSetup> setup, Action<ICodexNode> operation)
        {
            var entryPoint = CreateEntryPoint();
            // We have to be sure that the transient node we start is using the same image as whatever's already in the deployed network.
            // Therefore, we use the image of the bootstrap node.
            CodexDockerImage.Override = bootstrapNode.GetImageName();

            try
            {
                var debugInfo = bootstrapNode.GetDebugInfo();
                Assert.That(!string.IsNullOrEmpty(debugInfo.Spr));

                var node = entryPoint.CreateInterface().StartCodexNode(s =>
                {
                    setup(s);
                    s.WithBootstrapNode(bootstrapNode);
                });

                try
                {
                    operation(node);
                }
                catch
                {
                    node.DownloadLog();
                    throw;
                }
            }
            finally
            {
                entryPoint.Tools.CreateWorkflow().DeleteNamespace(wait: false);
            }
        }

        private EntryPoint CreateEntryPoint()
        {
            return entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, customNamespace, log);
        }
    }
}
