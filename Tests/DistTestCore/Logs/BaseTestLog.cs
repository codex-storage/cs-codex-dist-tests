using Logging;

namespace DistTestCore.Logs
{
    public abstract class BaseTestLog : BaseLog
    {
        private bool hasFailed;
        private readonly string deployId;

        protected BaseTestLog(string deployId)
        {
            this.deployId = deployId;
        }

        public void WriteLogTag()
        {
            var category = NameUtils.GetCategoryName();
            var name = NameUtils.GetTestMethodName();
            LogFile.WriteRaw($"{deployId} {category} {name}");
        }

        public void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
        }
    }
}
