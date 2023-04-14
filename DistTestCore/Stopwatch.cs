using Logging;
using Utils;

namespace DistTestCore
{
    public class Stopwatch
    {
        public static void Measure(BaseLog log, string name, Action action)
        {
            var start = DateTime.UtcNow;

            action();

            var duration = DateTime.UtcNow - start;

            log.Log($"{name} ({Time.FormatDuration(duration)})");
        }
    }
}
