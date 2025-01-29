using System.Diagnostics;
using CodexClient;
using Logging;

namespace CodexPlugin
{
    public class BinaryProcessControl : IProcessControl
    {
        private readonly Process process;
        private readonly CodexProcessConfig config;
        private readonly List<string> logLines = new List<string>();
        private readonly List<Task> streamTasks = new List<Task>();
        private bool running;

        public BinaryProcessControl(Process process, CodexProcessConfig config)
        {
            running = true;
            this.process = process;
            this.config = config;
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
            if (!Directory.Exists(config.DataDir)) throw new Exception("datadir not found");
            Directory.Delete(config.DataDir, true);
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            foreach (var line in logLines) file.WriteRaw(line);
            return new DownloadedLog(file, config.Name);
        }

        public bool HasCrashed()
        {
            return process.HasExited;
        }

        public void Stop(bool waitTillStopped)
        {
            running = false;
            process.Kill();

            if (waitTillStopped)
            {
                process.WaitForExit();
                foreach (var t in streamTasks) t.Wait();
            }

            DeleteDataDirFolder();
        }
    }
}
