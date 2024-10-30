using CodexOpenApi;
using Logging;
using Utils;

namespace AutoClient
{
    public interface ICodexInstance
    {
        string NodeId { get; }
        App App { get; }
        CodexApi Codex { get; }
        HttpClient Client { get; }
        Address Address { get; }
    }

    public class CodexInstance : ICodexInstance
    {
        public CodexInstance(App app, CodexApi codex, HttpClient client, Address address)
        {
            App = app;
            Codex = codex;
            Client = client;
            Address = address;
            NodeId = Guid.NewGuid().ToString();
        }

        public string NodeId { get; }
        public App App { get; }
        public CodexApi Codex { get; }
        public HttpClient Client { get; }
        public Address Address { get; }
    }
}
