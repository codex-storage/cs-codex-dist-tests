namespace Logging
{
    public class ConsoleLog : BaseLog
    {
        public override string GetFullName()
        {
            return "CONSOLE";
        }

        public override void Log(string message)
        {
            Console.WriteLine(ApplyReplacements(message));
        }
    }
}
