using Newtonsoft.Json;

namespace AutoClient.Modes.FolderStore
{
    public abstract class JsonBacked<T> where T : new()
    {
        private readonly App app;

        protected JsonBacked(App app, string folder, string filePath)
        {
            this.app = app;
            Folder = folder;
            FilePath = filePath;
            LoadState();
        }

        private void LoadState()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    State = new T();
                    OnNewState(State);
                    SaveState();
                }
                var text = File.ReadAllText(FilePath);
                State = JsonConvert.DeserializeObject<T>(text)!;
                if (State == null) throw new Exception("Didn't deserialize " + FilePath);
            }
            catch (Exception exc)
            {
                app.Log.Error("Failed to load state: " + exc);
            }
        }

        protected string Folder { get; }
        protected string FilePath { get; }
        protected T State { get; private set; } = default!;

        protected virtual void OnNewState(T newState)
        {
        }

        protected void SaveState()
        {
            try
            {
                var json = JsonConvert.SerializeObject(State);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception exc)
            {
                app.Log.Error("Failed to save state: " + exc);
            }
        }
    }
}
