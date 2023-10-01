using Logging;

namespace ContinuousTests
{
    public class ContainerLogStream
    {
        private readonly StreamReader reader;
        private readonly Stream stream;
        private readonly LogFile targetFile;
        private readonly CancellationToken token;
        private readonly TaskFactory taskFactory;
        private int lastNumber = -1;
        public bool Fault { get; private set; }
        private bool run;

        public ContainerLogStream(Stream stream, string name, LogFile targetFile, CancellationToken token, TaskFactory taskFactory)
        {
            this.stream = stream;
            this.targetFile = targetFile;
            this.token = token;
            this.taskFactory = taskFactory;
            Fault = false;
            reader = new StreamReader(stream);

            targetFile.Write(name);
        }

        public void Run()
        {
            run = true;
            taskFactory.Run(() => 
            {
                while (run && !token.IsCancellationRequested)
                {
                    Monitor();
                }
            });
        }

        public void Stop()
        {
            run = false;
            stream.Close();
        }

        public void DeleteFile()
        {
            if (run) throw new Exception("Cannot delete file while stream is still running.");
            File.Delete(targetFile.FullFilename);
        }

        private void Monitor()
        {
            var line = reader.ReadLine();
            while (run && !string.IsNullOrEmpty(line) && !token.IsCancellationRequested)
            {
                ProcessLine(line);
                line = reader.ReadLine();
            }
        }

        private void ProcessLine(string s)
        {
            targetFile.WriteRaw(s);

            // 000000004298
            var sub = s.Substring(0, 12);
            if (!int.TryParse(sub, out int number)) return;

            if (lastNumber == -1)
            {
                lastNumber = number;
            }
            else
            {
                var expectedNumber = lastNumber + 1;
                if (number != expectedNumber)
                {
                    Fault = true;
                }
                lastNumber = number;
            }
        }
    }
}
