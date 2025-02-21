using CodexClient;
using Utils;

namespace AutoClient
{
    public interface ICodexContext
    {
        string NodeId { get; }
        App App { get; }
        ICodexNode Codex { get; }
        HttpClient Client { get; }
        Address Address { get; }
    }

    public class CodexContext : ICodexContext
    {
        public CodexContext(App app, ICodexNode codex, HttpClient client, Address address)
        {
            App = app;
            Codex = codex;
            Client = client;
            Address = address;
            NodeId = Guid.NewGuid().ToString();
        }

        public string NodeId { get; }
        public App App { get; }
        public ICodexNode Codex { get; }
        public HttpClient Client { get; }
        public Address Address { get; }
    }
}
