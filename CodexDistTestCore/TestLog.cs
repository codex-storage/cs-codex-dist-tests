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

        public void EndTest()
        {
            var result = TestContext.CurrentContext.Result;

            Log($"Finished: {GetTestName()} = {result.Outcome.Status}");
            if (!string.IsNullOrEmpty(result.Message))
            {
                Log(result.Message);
                Log($"{result.StackTrace}");
            }

            if (result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                RenameLogFile();
            }
        }

        private void RenameLogFile()
        {
            file.ConcatToFilename("_FAILED");
        }

        public LogFile CreateSubfile(string ext = "log")
        {
            return new LogFile(now, $"{GetTestName()}_{subfileNumberSource.GetNextNumber().ToString().PadLeft(6, '0')}", ext);
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

    public class LogFile
    {
        private readonly DateTime now;
        private string name;
        private readonly string ext;
        private readonly string filepath;

        public LogFile(DateTime now, string name, string ext = "log")
        {
            this.now = now;
            this.name = name;
            this.ext = ext;

            filepath = Path.Join(
                LogConfig.LogRoot,
                $"{now.Year}-{Pad(now.Month)}",
                Pad(now.Day));

            Directory.CreateDirectory(filepath);

            GenerateFilename();
        }

        public string FullFilename { get; private set; } = string.Empty;
        public string FilenameWithoutPath { get; private set; } = string.Empty;

        public void Write(string message)
        {
            WriteRaw($"{GetTimestamp()} {message}");
        }

        public void WriteRaw(string message)
        {
            try
            {
                File.AppendAllLines(FullFilename, new[] { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing to log has failed: " + ex);
            }
        }

        public void ConcatToFilename(string toAdd)
        {
            var oldFullName = FullFilename;

            name += toAdd;

            GenerateFilename();

            File.Move(oldFullName, FullFilename);
        }

        private static string Pad(int n)
        {
            return n.ToString().PadLeft(2, '0');
        }

        private static string GetTimestamp()
        {
            return $"[{DateTime.UtcNow.ToString("u")}]";
        }

        private void GenerateFilename()
        {
            FilenameWithoutPath = $"{Pad(now.Hour)}-{Pad(now.Minute)}-{Pad(now.Second)}Z_{name.Replace('.', '-')}.{ext}";
            FullFilename = Path.Combine(filepath, FilenameWithoutPath);
        }
    }
}
