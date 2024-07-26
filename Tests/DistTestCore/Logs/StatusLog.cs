using Logging;
using Newtonsoft.Json;
using System.Globalization;

namespace DistTestCore.Logs
{
    public class StatusLog
    {
        private readonly object fileLock = new();
        private readonly string deployId;
        private readonly string fullName;
        private readonly string fixtureName;
        private readonly string testType;

        public StatusLog(LogConfig config, DateTime start, string testType, string deployId, string name = "")
        {
            fullName = NameUtils.GetFixtureFullName(config, start, name) + "_STATUS.log";
            fixtureName = NameUtils.GetRawFixtureName();
            this.testType = testType;
            this.deployId = deployId;
        }

        public void ConcludeTest(DistTestResult resultStatus, TimeSpan testDuration, Dictionary<string, string> data)
        {
            ConcludeTest(resultStatus.Status, testDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture), data);
        }

        public void ConcludeTest(string resultStatus, string testDuration, Dictionary<string, string> data)
        {
            data.Add("timestamp", DateTime.UtcNow.ToString("o"));
            data.Add("deployid", deployId);
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
