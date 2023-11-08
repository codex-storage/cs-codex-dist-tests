using Logging;

namespace DistTestCore.Logs
{
    public abstract class BaseTestLog : BaseLog
    {
        private bool hasFailed;

        public void WriteLogTag()
        {
            var runId = NameUtils.GetRunId();
            var category = NameUtils.GetCategoryName();
            var name = NameUtils.GetTestMethodName();
            LogFile.WriteRaw($"{runId} {category} {name}");
        }

        public void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
            LogFile.ConcatToFilename("_FAILED");
        }
    }
}
