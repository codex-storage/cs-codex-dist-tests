namespace Logging
{
    public abstract class BaseTestLog : BaseLog
    {
        private bool hasFailed;

        public BaseTestLog(bool debug)
            : base(debug)
        {
        }

        public void MarkAsFailed()
        {
            if (hasFailed) return;
            hasFailed = true;
            LogFile.ConcatToFilename("_FAILED");
        }
    }
}
