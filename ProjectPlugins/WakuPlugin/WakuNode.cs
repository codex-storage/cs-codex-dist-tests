using Core;
using KubernetesWorkflow.Types;

namespace WakuPlugin
{
    public interface IWakuNode : IHasContainer
    {
        DebugInfoResponse DebugInfo();
        void SubscribeToTopic(string topic);
        void SendMessage(string topic, string message);
        string[] GetMessages(string topic);
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
            return Api().HttpGetJson<DebugInfoResponse>("debug/v1/info");
        }

        public void SubscribeToTopic(string topic)
        {
            var response = Api().HttpPostString<string>(route: "relay/v1/subscriptions", body: topic);
        }

        public void SendMessage(string topic, string message)
        {
            var response = Api().HttpPostString<string>($"relay/v1/messages/{topic}", message);
        }

        public string[] GetMessages(string topic)
        {
            var response = Api().HttpGetString($"relay/v1/messages/{topic}");
            return new[] { "" };
        }

        private IEndpoint Api()
        {
            var address = Container.GetAddress(tools.GetLog(), WakuContainerRecipe.RestPortTag);
            return tools.CreateHttp().CreateEndpoint(address, "", logAlias: "waku");
        }
    }
}
