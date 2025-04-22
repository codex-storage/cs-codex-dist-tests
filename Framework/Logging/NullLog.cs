namespace Logging
{
    public class NullLog : BaseLog
    {
        public override string GetFullName()
        {
            return "NULL";
        }

        public override void Log(string message)
        {
            if (IsDebug) base.Log(message);
        }

        public override void Error(string message)
        {
            Console.WriteLine("Error: " + message);
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
