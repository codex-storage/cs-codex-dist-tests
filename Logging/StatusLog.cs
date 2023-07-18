using Newtonsoft.Json;

namespace Logging
{
    public class StatusLog
    {
        private readonly object fileLock = new object();
        private readonly string fullName;
        private readonly string fixtureName;
        private readonly string codexId;

        public StatusLog(LogConfig config, DateTime start, string codexId, string name = "")
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name);
            fixtureName = NameUtils.GetRawFixtureName();
            this.codexId = codexId;
        }

        public void TestPassed()
        {
            Write("successful");
        }

        public void TestFailed()
        {
            Write("failed");
        }

        private void Write(string status)
        {
            Write(new StatusLogJson
            {
                time = DateTime.UtcNow.ToString("u"),
                runid = GetRunId(),
                status = status,
                testid = GetTestId(),
                codexid = codexId,
                fixturename = fixtureName,
                testname = GetTestName()
            });
        }

        private string GetTestName()
        {
            return NameUtils.GetTestMethodName();
        }

        private string GetTestId()
        {
            return GetEnvVar("TESTID");
        }

        private string GetRunId()
        {
            return GetEnvVar("RUNID");
        }

        private string GetEnvVar(string name)
        {
            var v = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(v)) return $"'{name}' not set!";
            return v;
        }

        private void Write(StatusLogJson json)
        {
            try
            {
                lock (fileLock)
                {
                    File.AppendAllLines(fullName, new[] { JsonConvert.SerializeObject(json) });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to write to status log: " + ex);
            }
        }
    }

    public class StatusLogJson
    {
        public string time { get; set; } = string.Empty;
        public string runid { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string testid { get; set; } = string.Empty; 
        public string codexid { get; set; } = string.Empty;
        public string fixturename { get; set; } = string.Empty;
        public string testname { get; set; } = string.Empty;
    }
}
