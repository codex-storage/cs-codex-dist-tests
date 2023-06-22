using Logging;

namespace CodexNetDeployer
{
    public class NullLog : TestLog
    {
        public NullLog() : base("NULL", false, "NULL")
        {
        }

        protected override LogFile CreateLogFile()
        {
            return null!;
        }

        public override void Log(string message)
        {
        }

        public override void Debug(string message = "", int skipFrames = 0)
        {
        }

        public override void Error(string message)
        {
            Console.WriteLine("Error: " + message);
        }

        public override void MarkAsFailed()
        {
        }

        public override void AddStringReplace(string from, string to)
        {
        }

        public override void Delete()
        {
        }
    }
}
