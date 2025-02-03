using System.Diagnostics;
using CodexClient;
using Logging;

namespace CodexPlugin
{
    public class BinaryProcessControl : IProcessControl
    {
        private readonly LogFile logFile;
        private readonly Process process;
        private readonly CodexProcessConfig config;
        private List<string> logBuffer = new List<string>();
        private readonly object bufferLock = new object();
        private readonly List<Task> streamTasks = new List<Task>();
        private bool running;

        public BinaryProcessControl(ILog log, Process process, CodexProcessConfig config)
        {
            logFile = log.CreateSubfile(config.Name);

            running = true;
            this.process = process;
            this.config = config;
            streamTasks.Add(Task.Run(() => ReadProcessStream(process.StandardOutput)));
            streamTasks.Add(Task.Run(() => ReadProcessStream(process.StandardError)));
            streamTasks.Add(Task.Run(() => WriteLog()));
        }

        private void ReadProcessStream(StreamReader reader)
        {
            while (running)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    lock (bufferLock)
                    {
                        logBuffer.Add(line);
                    }
                }
            }
        }

        private void WriteLog()
        {
            while (running || logBuffer.Count > 0)
            {
                if (logBuffer.Count > 0)
                {
                    List<string> lines = null!;
                    lock (bufferLock)
                    {
                        lines = logBuffer;
                        logBuffer = new List<string>();
                    }

                    logFile.WriteRawMany(lines);
                }
                else Thread.Sleep(100);
            }
        }

        public void DeleteDataDirFolder()
        {
            if (!Directory.Exists(config.DataDir)) throw new Exception("datadir not found");
            Directory.Delete(config.DataDir, true);
        }

        public IDownloadedLog DownloadLog(LogFile file)
        {
            return new DownloadedLog(logFile, config.Name);
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
