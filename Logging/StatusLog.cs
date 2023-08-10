using Newtonsoft.Json;

namespace Logging
{
    public class StatusLog
    {
        private readonly object fileLock = new object();
        private readonly string fullName;
        private readonly string fixtureName;

        public StatusLog(LogConfig config, DateTime start, string name = "")
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name) + "_STATUS.log";
            fixtureName = NameUtils.GetRawFixtureName();
        }

        public void ConcludeTest(string resultStatus, string testDuration, ApplicationIds applicationIds)
        {
            Write(new StatusLogJson
            {
                @timestamp = DateTime.UtcNow.ToString("o"),
                runid = NameUtils.GetRunId(),
                status = resultStatus,
                testid = NameUtils.GetTestId(),
                codexid = applicationIds.CodexId,
                gethid = applicationIds.GethId,
                prometheusid = applicationIds.PrometheusId,
                codexcontractsid = applicationIds.CodexContractsId,
                category = NameUtils.GetCategoryName(),
                fixturename = fixtureName,
                testname = NameUtils.GetTestMethodName(),
                testduration = testDuration
            });
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
        public string @timestamp { get; set; } = string.Empty;
        public string runid { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string testid { get; set; } = string.Empty; 
        public string codexid { get; set; } = string.Empty;
        public string gethid { get; set; } = string.Empty;
        public string prometheusid { get; set; } = string.Empty;
        public string codexcontractsid { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string fixturename { get; set; } = string.Empty;
        public string testname { get; set; } = string.Empty;
        public string testduration { get; set;} = string.Empty;
    }
}
