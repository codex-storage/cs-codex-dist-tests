namespace AutoClientCenter
{
    public interface ITaskService
    {
        AcStats GetStats();
        AcTasks GetTasks();
        void ProcessResults(AcTaskStep[] taskSteps);
        void SetConfig(AcTasks tasks);
    }

    public class TaskService : ITaskService
    {
        private readonly List<long> downloadTimes = new List<long>();
        private readonly AcStats stats = new AcStats
        {
            ServiceStartUtc = DateTime.UtcNow,
        };

        private AcTasks tasks = new AcTasks
        {
            StartTaskEvery = TimeSpan.FromHours(8),
            Tasks = Array.Empty<AcTask>()
        };

        public AcStats GetStats()
        {
            return stats;
        }

        public AcTasks GetTasks()
        {
            return tasks;
        }

        public void SetConfig(AcTasks newTasks)
        {
            if (newTasks.StartTaskEvery < TimeSpan.FromMinutes(10)) return;
            foreach (var task in newTasks.Tasks)
            {
                if (task.ChanceWeight < 1) return;
                foreach (var step in task.Steps)
                {
                    if (string.IsNullOrWhiteSpace(step.Id)) return;
                    if (step.UploadStep == null && step.StoreStep == null && step.DownloadStep == null) return;
                }
            }

            tasks = newTasks;
        }

        public void ProcessResults(AcTaskStep[] taskSteps)
        {
            throw new NotImplementedException();
        }
    }
}
