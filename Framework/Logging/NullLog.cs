namespace Logging
{
    public class NullLog : BaseLog
    {
        public NullLog() : base(false)
        {
        }

        public string FullFilename { get; set; } = "NULL";

        protected override string GetFullName()
        {
            return FullFilename;
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

        public override void AddStringReplace(string from, string to)
        {
        }

        public override void Delete()
        {
        }
    }
}
