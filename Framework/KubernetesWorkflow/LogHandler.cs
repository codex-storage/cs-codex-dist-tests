namespace KubernetesWorkflow
{
    public interface ILogHandler
    {
        void Log(Stream log);
    }

    public abstract class LogHandler : ILogHandler
    {
        public void Log(Stream log)
        {
            using var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                ProcessLine(line);
                line = reader.ReadLine();
            }
        }

        protected abstract void ProcessLine(string line);
    }
}
