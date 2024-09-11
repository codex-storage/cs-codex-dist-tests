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
        private readonly CidRepo cidRepo = new CidRepo();
        private readonly List<long> downloadTimes = new List<long>();
        private readonly AcStats stats = new AcStats
        {
            ServiceStartUtc = DateTime.UtcNow,
        };

        private AcTasks tasks = new AcTasks
        {
            StartTaskEverySeconds = Convert.ToInt32(TimeSpan.FromHours(8).TotalSeconds),
            Tasks = Array.Empty<AcTask>()
        };

        public AcStats GetStats()
        {
            stats.DownloadTimesMillisecondsPerKb = downloadTimes.ToArray();
            return stats;
        }

        public AcTasks GetTasks()
        {
            foreach (var task in tasks.Tasks)
            {
                foreach (var step in task.Steps)
                {
                    if (step.DownloadStep != null) cidRepo.Assign(step.DownloadStep);
                }
            }
            return tasks;
        }

        public void SetConfig(AcTasks newTasks)
        {
            if (newTasks.StartTaskEverySeconds < (10 * 60)) return;
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
            foreach (var step in taskSteps) ProcessResults(step);
        }

        private void ProcessResults(AcTaskStep step)
        {
            ProcessResult(step.UploadStep);
            ProcessResult(step.StoreStep);
            ProcessResult(step.DownloadStep);
        }

        private void ProcessResult(AcUploadStep? uploadStep)
        {
            if (uploadStep == null) return;

            if (string.IsNullOrWhiteSpace(uploadStep.ResultCid))
            {
                stats.TotalUploadsFailed++;
            }
            else
            {
                stats.TotalUploads++;
                cidRepo.Add(uploadStep.ResultCid, uploadStep.SizeInBytes);
            }
        }

        private void ProcessResult(AcStoreStep? storeStep)
        {
            if (storeStep == null) return;

            if (string.IsNullOrWhiteSpace(storeStep.ResultOriginalCid) ||
                string.IsNullOrWhiteSpace(storeStep.ResultEncodedCid) ||
                string.IsNullOrWhiteSpace(storeStep.ResultPurchaseId))
            {
                stats.TotalContractStartsFailed++;
            }
            else
            {
                stats.TotalContractsStarted++;
                cidRepo.AddEncoded(storeStep.ResultOriginalCid, storeStep.ResultEncodedCid);
            }
        }

        private void ProcessResult(AcDownloadStep? downloadStep)
        {
            if (downloadStep == null) return;

            var kbs = cidRepo.GetSizeKbsForCid(downloadStep.Cid);
            if (kbs == null) return;
            var milliseconds = downloadStep.ResultDownloadTimeMilliseconds;

            var millisecondsPerKb = milliseconds / kbs.Value;
            downloadTimes.Add(millisecondsPerKb);
        }
    }
}
