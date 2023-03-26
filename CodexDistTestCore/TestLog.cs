using CodexDistTestCore.Config;
using NUnit.Framework;

namespace CodexDistTestCore
{
    public class TestLog
    {
        private readonly LogFile file;

        public TestLog()
        {
            var name = GetTestName();
            file = new LogFile(name);

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

                var logWriter = new PodLogWriter(file);
                logWriter.IncludeFullPodLogging(k8sManager);
            }
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
            var logFile = id.ToString().PadLeft(6, '0');
            file.Write($"{podDescription} -->> {logFile}");
            LogRaw(podDescription, logFile);
            var reader = new StreamReader(log);
            var line = reader.ReadLine();
            while (line != null)
            {
                LogRaw(line, logFile);
                line = reader.ReadLine();
            }
        }

        private void LogRaw(string message, string filename)
        {
            file!.WriteRaw(message, filename);
        }
    }

    public class LogFile
    {
        private readonly string filepath;
        private readonly string filename;

        public LogFile(string name)
        {
            var now = DateTime.UtcNow;

            filepath = Path.Join(
                LogConfig.LogRoot,
                $"{now.Year}-{Pad(now.Month)}",
                Pad(now.Day));

            Directory.CreateDirectory(filepath);

            filename = Path.Combine(filepath, $"{Pad(now.Hour)}-{Pad(now.Minute)}-{Pad(now.Second)}Z_{name.Replace('.', '-')}");
        }

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message, string subfile = "")
        {
            try
            {
                File.AppendAllLines(filename + subfile + ".log", new[] { message });
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
