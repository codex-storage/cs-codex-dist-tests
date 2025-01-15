using Utils;

namespace CodexPlugin
{
    public interface ICodexInstance
    {
        string Name { get; }
        string ImageName { get; }
        DateTime StartUtc { get; }
        Address DiscoveryEndpoint { get; }
        Address ApiEndpoint { get; }
        void DeleteDataDirFolder();
    }
}
