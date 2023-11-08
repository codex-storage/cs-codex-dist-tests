namespace Logging
{
    public class ConsoleLog : BaseLog
    {
        protected override string GetFullName()
        {
            return "CONSOLE";
        }

        public override void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
