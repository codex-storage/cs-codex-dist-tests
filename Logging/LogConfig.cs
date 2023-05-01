namespace Logging
{
    public class LogConfig
    {
        public LogConfig(string logRoot, bool debugEnabled)
        {
            LogRoot = logRoot;
            DebugEnabled = debugEnabled;
        }
        
        public string LogRoot { get; }
        public bool DebugEnabled { get; }
    }
}
