namespace Logging
{
    public class LogConfig
    {
        public LogConfig(string logRoot)
        {
            LogRoot = logRoot;
        }
        
        public string LogRoot { get; }
    }
}
