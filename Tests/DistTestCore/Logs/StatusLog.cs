using Logging;
using Newtonsoft.Json;
using Utils;

namespace DistTestCore.Logs
{
    public class StatusLog
    {
        private readonly object fileLock = new object();
        private readonly string fullName;
        private readonly string fixtureName;
        private readonly string testType;

        public StatusLog(LogConfig config, DateTime start, string testType, string name = "")
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name) + "_STATUS.log";
            fixtureName = NameUtils.GetRawFixtureName();
            this.testType = testType;
        }

        public void ConcludeTest(string resultStatus, TimeSpan testDuration, Dictionary<string, string> data)
        {
            ConcludeTest(resultStatus, Time.FormatDuration(testDuration), data);
        }

        public void ConcludeTest(string resultStatus, string testDuration, Dictionary<string, string> data)
        {
            data.Add("timestamp", DateTime.UtcNow.ToString("o"));
            data.Add("runid", NameUtils.GetRunId());
            data.Add("status", resultStatus);
            data.Add("category", NameUtils.GetCategoryName());
            data.Add("fixturename", fixtureName);
            if (!data.ContainsKey("testname")) data.Add("testname", NameUtils.GetTestMethodName());
            data.Add("testid", NameUtils.GetTestId());
            data.Add("testtype", testType);
            data.Add("testduration", testDuration);
            data.Add("testframeworkrevision", GitInfo.GetStatus());
            Write(data);
        }

        private void Write(Dictionary<string, string> data)
        {
            try
            {
                lock (fileLock)
                {
                    File.AppendAllLines(fullName, new[] { JsonConvert.SerializeObject(data) });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to write to status log: " + ex);
            }
        }
    }
}
