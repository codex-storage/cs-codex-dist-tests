using NUnit.Framework;

namespace CodexDistTestCore
{
    public class TestLog
    {
        public const string LogRoot = "D:/CodexTestLogs";

        private static LogFile? file = null;

        // This is all way too static. It needs to be cleaned up.
        public static void Log(string message)
        {
            file!.Write(message);
        }

        public static void Error(string message)
        {
            Log($"[ERROR] {message}");
        }

        public static void BeginTest()
        {
            if (file != null) throw new InvalidOperationException("Test is already started!");

            var name = GetTestName();
            file = new LogFile(name);

            Log($"Begin: {name}");
        }

        public static void EndTest(K8sManager k8sManager)
        {
            if (file == null) throw new InvalidOperationException("No test is started!");


            var result = TestContext.CurrentContext.Result;

            Log($"Finished: {GetTestName()} = {result.Outcome.Status}");
            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                var logWriter = new PodLogWriter(file);
                logWriter.IncludeFullPodLogging(k8sManager);
            }

            file = null;
        }

        private static string GetTestName()
        {
            var test = TestContext.CurrentContext.Test;
            var className = test.ClassName!.Substring(test.ClassName.LastIndexOf('.') + 1);
            return $"{className}.{test.MethodName}";
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
            TestLog.Log("Full pod logging:");
            k8sManager.FetchAllPodsLogs(this);
        }

        public void Log(int id, string podDescription, Stream log)
        {
            var logFile = id.ToString().PadLeft(6, '0');
            TestLog.Log($"{podDescription} -->> {logFile}");
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
                TestLog.LogRoot,
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
