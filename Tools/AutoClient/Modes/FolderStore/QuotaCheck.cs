using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class QuotaCheck
    {
        private readonly ILog log;
        private readonly string filepath;
        private readonly CodexWrapper instance;

        public QuotaCheck(ILog log, string filepath, CodexWrapper instance)
        {
            this.log = log;
            this.filepath = filepath;
            this.instance = instance;
        }

        public bool IsLocalQuotaAvailable()
        {
            try
            {
                return CheckQuota();
            }
            catch (Exception exc)
            {
                log.Error("Failed to check quota: " + exc);
                throw;
            }
        }

        private bool CheckQuota()
        { 
            var info = new FileInfo(filepath);
            var fileSize = info.Length;
            var padded = new ByteSize(Convert.ToInt64(fileSize * 1.1));
            var space = instance.Node.Space();
            var free = new ByteSize(space.FreeBytes);

            log.Log($"Quota free: {free} - filesize: {padded}");
            return free.SizeInBytes > padded.SizeInBytes;
        }
    }
}
