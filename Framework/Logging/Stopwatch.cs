using Utils;

namespace Logging
{
    public class Stopwatch
    {
        private readonly DateTime start = DateTime.UtcNow;
        private readonly ILog log;
        private readonly string name;
        private readonly bool debug;

        private Stopwatch(ILog log, string name, bool debug)
        {
            this.log = log;
            this.name = name;
            this.debug = debug;
        }

        public static TimeSpan Measure(ILog log, string name, Action action, bool debug = false)
        {
            var sw = Begin(log, name, debug);
            action();
            return sw.End();
        }

        public static StopwatchResult<T> Measure<T>(ILog log, string name, Func<T> action, bool debug = false)
        {
            var sw = Begin(log, name, debug);
            var result = action();
            var duration = sw.End();
            return new StopwatchResult<T>(result, duration);
        }

        public static Stopwatch Begin(ILog log)
        {
            return Begin(log, "");
        }

        public static Stopwatch Begin(ILog log, string name)
        {
            return Begin(log, name, false);
        }

        public static Stopwatch Begin(ILog log, bool debug)
        {
            return Begin(log, "", debug);
        }

        public static Stopwatch Begin(ILog log, string name, bool debug)
        {
            return new Stopwatch(log, name, debug);
        }

        public TimeSpan End(string msg = "", int skipFrames = 0)
        {
            var duration = DateTime.UtcNow - start;
            var entry = $"{name} {msg} ({Time.FormatDuration(duration)})";

            if (debug)
            {
                log.Debug(entry, 1 + skipFrames);
            }
            else
            {
                log.Log(entry);
            }

            return duration;
        }
    }

    public class StopwatchResult<T>
    {
        public StopwatchResult(T value, TimeSpan duration)
        {
            Value = value;
            Duration = duration;
        }

        public T Value { get; }
        public TimeSpan Duration { get; }
    }
}
