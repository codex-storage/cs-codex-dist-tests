using Core;
using KubernetesWorkflow;

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
            return Http().HttpGetJson<DebugInfoResponse>("debug/v1/info");
        }

        public void SubscribeToTopic(string topic)
        {
            var response = Http().HttpPostString("relay/v1/subscriptions", topic);
        }

        public void SendMessage(string topic, string message)
        {
            var response = Http().HttpPostString($"relay/v1/messages/{topic}", message);
        }

        public string[] GetMessages(string topic)
        {
            var response = Http().HttpGetString($"relay/v1/messages/{topic}");
            return new[] { "" };
        }

        private IHttp Http()
        {
            return tools.CreateHttp(Container.Address, "");
        }
    }
}
