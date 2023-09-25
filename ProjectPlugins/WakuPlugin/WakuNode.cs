using Core;
using KubernetesWorkflow;

namespace WakuPlugin
{
    public interface IWakuNode : IHasContainer
    {
        DebugInfoResponse DebugInfo();
    }

    public class WakuNode : IWakuNode
    {
        private readonly IPluginTools tools;

        public WakuNode(IPluginTools tools, RunningContainer container)
        {
            this.tools = tools;
            Container = container;
        }

        public RunningContainer Container { get; }

        public DebugInfoResponse DebugInfo()
        {
            return Http().HttpGetJson<DebugInfoResponse>("debug/v1/info");
        }

        private IHttp Http()
        {
            return tools.CreateHttp(Container.Address, "");
        }
    }
}
