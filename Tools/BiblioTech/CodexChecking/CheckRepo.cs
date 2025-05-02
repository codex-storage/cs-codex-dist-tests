using Newtonsoft.Json;

namespace BiblioTech.CodexChecking
{
    public class CheckRepo
    {
        private const string modelFilename = "model.json";
        private readonly Configuration config;
        private readonly object _lock = new object();
        private CheckRepoModel? model = null;

        public CheckRepo(Configuration config)
        {
            this.config = config;
        }

        public CheckReport GetOrCreate(ulong userId)
        {
            lock (_lock)
            {
                if (model == null) LoadModel();

                var existing = model.Reports.SingleOrDefault(r  => r.UserId == userId);
                if (existing == null)
                {
                    var newEntry = new CheckReport
                    {
                        UserId = userId,
                    };
                    model.Reports.Add(newEntry);
                    SaveChanges();
                    return newEntry;
                }
                return existing;
            }
        }

        public void SaveChanges()
        {
            File.WriteAllText(GetModelFilepath(), JsonConvert.SerializeObject(model, Formatting.Indented));
        }

        private void LoadModel()
        {
            if (!File.Exists(GetModelFilepath()))
            {
                model = new CheckRepoModel();
                SaveChanges();
                return;
            }

            model = JsonConvert.DeserializeObject<CheckRepoModel>(File.ReadAllText(GetModelFilepath()));
        }

        private string GetModelFilepath()
        {
            return Path.Combine(config.ChecksDataPath, modelFilename);
        }
    }

    public class CheckRepoModel
    {
        public List<CheckReport> Reports { get; set; } = new List<CheckReport>();
    }

    public class CheckReport
    {
        public ulong UserId { get; set; }
        public TransferCheck UploadCheck { get; set; } = new TransferCheck();
        public TransferCheck DownloadCheck { get; set; } = new TransferCheck();
    }

    public class TransferCheck
    {
        public DateTime CompletedUtc { get; set; } = DateTime.MinValue;
        public string UniqueData { get; set; } = string.Empty;
    }
}
