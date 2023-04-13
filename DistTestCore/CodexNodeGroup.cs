using DistTestCore.Codex;
using KubernetesWorkflow;
using System.Collections;

namespace DistTestCore
{
    public interface ICodexNodeGroup : IEnumerable<IOnlineCodexNode>
    {
        //ICodexSetup BringOffline();
        IOnlineCodexNode this[int index] { get; }
    }

    public class CodexNodeGroup : ICodexNodeGroup
    {
        private readonly TestLifecycle lifecycle;

        public CodexNodeGroup(TestLifecycle lifecycle, CodexSetup setup, RunningContainers containers)
        {
            this.lifecycle = lifecycle;
            Setup = setup;
            Containers = containers;
            Nodes = containers.Containers.Select(c => CreateOnlineCodexNode(c)).ToArray();
        }

        public IOnlineCodexNode this[int index]
        {
            get
            {
                return Nodes[index];
            }
        }

        //public ICodexSetup BringOffline()
        //{
        //    //return k8SManager.BringOffline(this);
        //}

        public CodexSetup Setup { get; }
        public RunningContainers Containers { get; }
        public OnlineCodexNode[] Nodes { get; }

        //public GethCompanionGroup? GethCompanionGroup { get; set; }

        //public CodexNodeContainer[] GetContainers()
        //{
        //    return Nodes.Select(n => n.Container).ToArray();
        //}

        public IEnumerator<IOnlineCodexNode> GetEnumerator()
        {
            return Nodes.Cast<IOnlineCodexNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        //public CodexNodeLog DownloadLog(IOnlineCodexNode node)
        //{
        //    var logDownloader = new PodLogDownloader(log, k8SManager);
        //    var n = (OnlineCodexNode)node;
        //    return logDownloader.DownloadLog(n);
        //}

        public string Describe()
        {
            var orderNumber = Containers.RunningPod.Ip;
            return $"CodexNodeGroup@{orderNumber}-{Setup.Describe()}";
        }

        private OnlineCodexNode CreateOnlineCodexNode(RunningContainer c)
        {
            var access = new CodexAccess(c);
            return new OnlineCodexNode(lifecycle, access, this);
        }
    }
}
