using CodexPlugin;
using DistTestCore;
using DistTestCore.Helpers;

namespace Tests
{
    public class CodexDistTest : DistTest
    {
        private readonly List<IOnlineCodexNode> onlineCodexNodes = new List<IOnlineCodexNode>();

        public IOnlineCodexNode AddCodex()
        {
            return AddCodex(s => { });
        }

        public IOnlineCodexNode AddCodex(Action<ICodexSetup> setup)
        {
            return AddCodex(1, setup)[0];
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes)
        {
            return AddCodex(numberOfNodes, s => { });
        }

        public ICodexNodeGroup AddCodex(int numberOfNodes, Action<ICodexSetup> setup)
        {
            var group = Ci.SetupCodexNodes(numberOfNodes, s =>
            {
                setup(s);
                OnCodexSetup(s);
            });
            onlineCodexNodes.AddRange(group);
            return group;
        }

        public PeerConnectionTestHelpers CreatePeerConnectionTestHelpers()
        {
            return new PeerConnectionTestHelpers(GetTestLog());
        }

        public PeerDownloadTestHelpers CreatePeerDownloadTestHelpers()
        {
            return new PeerDownloadTestHelpers(GetTestLog(), Get().GetFileManager());
        }

        public IEnumerable<IOnlineCodexNode> GetAllOnlineCodexNodes()
        {
            return onlineCodexNodes;
        }

        protected virtual void OnCodexSetup(ICodexSetup setup)
        {
        }
    }
}
