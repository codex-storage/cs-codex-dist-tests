using NUnit.Framework;

namespace CodexDistTests.TestCore
{
    public class TestLog
    {
        public const string LogRoot = "D:/CodexTestLogs";

        private static LogFile? file = null;

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
                IncludeFullPodLogging(k8sManager);
            }

            LogRaw("");
            file = null;
        }

        private static string GetTestName()
        {
            var test = TestContext.CurrentContext.Test;
            var className = test.ClassName!.Substring(test.ClassName.LastIndexOf('.') + 1);
            return $"{className}.{test.MethodName}";
        }

        private static void LogRaw(string message)
        {
            file!.WriteRaw(message);
        }

        private static void IncludeFullPodLogging(K8sManager k8sManager)
        {
            LogRaw("Full pod logging:");
            k8sManager.FetchAllPodsLogs(WritePodLog);
        }

        private static void WritePodLog(string nodeDescription, Stream stream)
        {
            LogRaw("---");
            LogRaw(nodeDescription);
            var reader = new StreamReader(stream);
            var line = reader.ReadLine();
            while (line != null)
            {
                LogRaw(line);
                line = reader.ReadLine();
            }
        }
    }

    public class LogFile
    {
        private readonly string filename;

        public LogFile(string name)
        {
            var now = DateTime.UtcNow;

            var filepath = Path.Join(
                TestLog.LogRoot,
                $"{now.Year}-{Pad(now.Month)}",
                Pad(now.Day));

            Directory.CreateDirectory(filepath);

            filename = Path.Combine(filepath,
                $"{Pad(now.Hour)}-{Pad(now.Minute)}-{Pad(now.Second)}Z_{name.Replace('.', '-')}.log");
        }

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message)
        {
            try
            {
                File.AppendAllLines(filename, new[] {  message });
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
