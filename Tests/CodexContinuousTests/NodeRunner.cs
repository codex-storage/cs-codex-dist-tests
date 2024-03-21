using NUnit.Framework;
using Logging;
using Utils;
using Core;
using CodexPlugin;
using KubernetesWorkflow.Types;

namespace ContinuousTests
{
    public class NodeRunner
    {
        private readonly EntryPointFactory entryPointFactory = new EntryPointFactory();
        private readonly ICodexNode[] nodes;
        private readonly Configuration config;
        private readonly ILog log;

        public NodeRunner(ICodexNode[] nodes, Configuration config, ILog log)
        {
            this.nodes = nodes;
            this.config = config;
            this.log = log;
        }

        public IDownloadedLog DownloadLog(RunningContainer container, int? tailLines = null)
        {
            var entryPoint = CreateEntryPoint();
            return entryPoint.CreateInterface().DownloadLog(container, tailLines);
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
            CodexContainerRecipe.DockerImageOverride = bootstrapNode.Container.Recipe.Image;

            var debugInfo = bootstrapNode.GetDebugInfo();
            Assert.That(!string.IsNullOrEmpty(debugInfo.spr));

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
                DownloadLog(node.Container);
                node.Stop();
                throw;
            }
            finally
            {
                node.Stop();
            }
        }

        private EntryPoint CreateEntryPoint()
        {
            return entryPointFactory.CreateEntryPoint(config.KubeConfigFile, config.DataPath, config.CodexDeployment.Metadata.KubeNamespace, log);
        }
    }
}
