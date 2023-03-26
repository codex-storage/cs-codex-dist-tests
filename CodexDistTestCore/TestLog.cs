using CodexDistTestCore.Config;
using NUnit.Framework;

namespace CodexDistTestCore
{
    public class TestLog
    {
        private readonly NumberSource subfileNumberSource = new NumberSource(0);
        private readonly LogFile file;
        private readonly DateTime now;

        public TestLog()
        {
            now = DateTime.UtcNow;

            var name = GetTestName();
            file = new LogFile(now, name);

            Log($"Begin: {name}");
        }

        public void Log(string message)
        {
            file.Write(message);
        }

        public void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public void EndTest(K8sManager k8sManager)
        {
            var result = TestContext.CurrentContext.Result;

            Log($"Finished: {GetTestName()} = {result.Outcome.Status}");
            
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
            }

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                Log($"{result.StackTrace}");

                var logWriter = new PodLogWriter(CreateSubfile());
                logWriter.IncludeFullPodLogging(k8sManager);
            }
        }

        public LogFile CreateSubfile()
        {
            return new LogFile(now, $"{GetTestName()}_{subfileNumberSource.GetNextNumber().ToString().PadLeft(6, '0')}");
        }

        private static string GetTestName()
        {
            var test = TestContext.CurrentContext.Test;
            var className = test.ClassName!.Substring(test.ClassName.LastIndexOf('.') + 1);
            var args = FormatArguments(test);
            return $"{className}.{test.MethodName}{args}";
        }

        private static string FormatArguments(TestContext.TestAdapter test)
        {
            if (test.Arguments == null || !test.Arguments.Any()) return "";
            return $"[{string.Join(',', test.Arguments)}]";
        }
    }

    public class PodLogWriter : IPodLogsHandler
    {
        private readonly LogFile file;

        public PodLogWriter(LogFile file)
        {
            this.file = file;
        }

        public void IncludeFullPodLogging(K8sManager k8sManager)
        {
            file.Write("Full pod logging:");
            k8sManager.FetchAllPodsLogs(this);
        }

        public void Log(int id, string podDescription, Stream log)
        {
            file.Write($"{podDescription} -->> {file.FilenameWithoutPath}");
            LogRaw(podDescription);
            var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                LogRaw(line);
                line = reader.ReadLine();
            }
        }

        private void LogRaw(string message)
        {
            file!.WriteRaw(message);
        }
    }

    public class LogFile
    {
        private readonly string filepath;
        private readonly string filename;

        public LogFile(DateTime now, string name)
        {
            filepath = Path.Join(
                LogConfig.LogRoot,
                $"{now.Year}-{Pad(now.Month)}",
                Pad(now.Day));

            Directory.CreateDirectory(filepath);

            FilenameWithoutPath = $"{Pad(now.Hour)}-{Pad(now.Minute)}-{Pad(now.Second)}Z_{name.Replace('.', '-')}.log";

            filename = Path.Combine(filepath, FilenameWithoutPath);
        }

        public string FilenameWithoutPath { get; }

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message)
        {
            try
            {
                File.AppendAllLines(filename, new[] { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        private static string Pad(int n)
        {
            return n.ToString().PadLeft(2, '0');
        }

        private static string GetTimestamp()
        {
            return $"[{DateTime.UtcNow.ToString("u")}]";
        }
    }
}
