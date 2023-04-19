using Logging;
using Utils;

namespace DistTestCore
{
    public class Stopwatch
    {
        private readonly DateTime start = DateTime.UtcNow;
        private readonly BaseLog log;
        private readonly string name;

        public Stopwatch(BaseLog log, string name)
        {
            this.log = log;
            this.name = name;
        }

        public static void Measure(BaseLog log, string name, Action action)
        {
            var sw = Begin(log, name);
            action();
            sw.End();
        }

        public static Stopwatch Begin(BaseLog log, string name)
        {
            return new Stopwatch(log, name);
        }

        public void End(string msg = "")
        {
            var duration = DateTime.UtcNow - start;
            log.Log($"{name} {msg} ({Time.FormatDuration(duration)})");
        }
    }
}
