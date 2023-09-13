namespace Core
{
    public interface IPluginMetadata
    {
        Dictionary<string, string> Get();
    }

    public interface IAddMetadata
    {
        void Add(string key, string value);
    }

    public class PluginMetadata : IPluginMetadata, IAddMetadata
    {
        private readonly Dictionary<string, string> metadata = new Dictionary<string, string>();

        public void Add(string key, string value)
        {
            metadata.Add(key, value);
        }

        public Dictionary<string, string> Get()
        {
            return new Dictionary<string, string>(metadata);
        }
    }
}
