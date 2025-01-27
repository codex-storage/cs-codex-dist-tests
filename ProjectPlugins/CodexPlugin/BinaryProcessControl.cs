using System.Diagnostics;
using CodexClient;
using Logging;

namespace CodexPlugin
{
    public class BinaryProcessControl : IProcessControl
    {
        private Process process;
        private readonly string nodeName;
        private bool running;
        private readonly List<string> logLines = new List<string>();
        private readonly List<Task> streamTasks = new List<Task>();

        public BinaryProcessControl(Process process, string nodeName)
        {
            running = true;
            this.process = process;
            this.nodeName = nodeName;
            streamTasks.Add(Task.Run(() => ReadProcessStream(process.StandardOutput)));
            streamTasks.Add(Task.Run(() => ReadProcessStream(process.StandardError)));
        }

        private void ReadProcessStream(StreamReader reader)
        {
            while (running)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line)) logLines.Add(line);
            }
        }

        public void DeleteDataDirFolder()
        {
            throw new NotImplementedException();
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            foreach (var line in logLines) file.WriteRaw(line);
            return new DownloadedLog(file, nodeName);
        }

        public bool HasCrashed()
        {
            return false;
        }

        public void Stop(bool waitTillStopped)
        {
            running = false;
            process.Kill();
            foreach (var t in streamTasks) t.Wait();
        }
    }
}
