using Newtonsoft.Json;

namespace AutoClient.Modes.FolderStore
{
    public class JsonFile<T> where T : new()
    {
        private readonly App app;
        private readonly string filePath;
        private readonly object fileLock = new object();

        public JsonFile(App app, string filePath)
        {
            this.app = app;
            this.filePath = filePath;
        }

        public T Load()
        {
            lock (fileLock)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        var state = new T();
                        Save(state);
                        return state;
                    }
                    var text = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(text)!;
                }
                catch (Exception exc)
                {
                    app.Log.Error("Failed to load state: " + exc);
                    throw;
                }
            }
        }

        public void Save(T state)
        {
            lock (fileLock)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(state, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception exc)
                {
                    app.Log.Error("Failed to save state: " + exc);
                    throw;
                }
            }
        }
    }
}
