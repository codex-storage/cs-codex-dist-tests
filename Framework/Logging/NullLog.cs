namespace Logging
{
    public class NullLog : BaseLog
    {
        public string FullFilename { get; set; } = "NULL";

        protected override string GetFullName()
        {
            return FullFilename;
        }

        public override void Log(string message)
        {
            if (IsDebug) base.Log(message);
        }

        public override void Error(string message)
        {
            base.Error(message);
        }

        public override void AddStringReplace(string from, string to)
        {
        }

        public override void Delete()
        {
        }
    }
}
