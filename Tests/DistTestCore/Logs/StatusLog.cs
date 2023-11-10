using Logging;
using Newtonsoft.Json;

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

        public void ConcludeTest(string resultStatus, string testDuration, Dictionary<string, string> data)
        {
            data.Add("timestamp", DateTime.UtcNow.ToString("o"));
            data.Add("runid", NameUtils.GetRunId());
            data.Add("status", resultStatus);
            data.Add("category", NameUtils.GetCategoryName());
            data.Add("fixturename", fixtureName);
            data.Add("testid", NameUtils.GetTestId());
            data.Add("testname", NameUtils.GetTestMethodName());
            data.Add("testtype", testType);
            data.Add("testduration", testDuration);
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
