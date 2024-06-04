
namespace Utils
{
    public class Retry<T>
    {
        private readonly string description;
        private readonly Func<T> task;
        private readonly TimeSpan maxTimeout;
        private readonly int maxRetries;
        private readonly TimeSpan sleepAfterFail;
        private readonly Action<Failure> onFail;

        public Retry(string description, Func<T> task, TimeSpan maxTimeout, int maxRetries, TimeSpan sleepAfterFail, Action<Failure> onFail)
        {
            this.description = description;
            this.task = task;
            this.maxTimeout = maxTimeout;
            this.maxRetries = maxRetries;
            this.sleepAfterFail = sleepAfterFail;
            this.onFail = onFail;
        }

        public T Run()
        {
            var run = new RetryRun(description, task, maxTimeout, maxRetries, sleepAfterFail, onFail);
            return run.Run();
        }

        private class RetryRun
        {
            private readonly string description;
            private readonly Func<T> task;
            private readonly TimeSpan maxTimeout;
            private readonly int maxRetries;
            private readonly TimeSpan sleepAfterFail;
            private readonly Action<Failure> onFail;
            private readonly DateTime start = DateTime.UtcNow;
            private readonly List<Failure> failures = new List<Failure>();
            private int tryNumber;
            private DateTime tryStart;

            public RetryRun(string description, Func<T> task, TimeSpan maxTimeout, int maxRetries, TimeSpan sleepAfterFail, Action<Failure> onFail)
            {
                this.description = description;
                this.task = task;
                this.maxTimeout = maxTimeout;
                this.maxRetries = maxRetries;
                this.sleepAfterFail = sleepAfterFail;
                this.onFail = onFail;

                tryNumber = 0;
                tryStart = DateTime.UtcNow;
            }

            public T Run()
            {
                while (true)
                {
                    CheckMaximums();

                    tryNumber++;
                    tryStart = DateTime.UtcNow;
                    try
                    {
                        return task();
                    }
                    catch (Exception ex)
                    {
                        var failure = CaptureFailure(ex);
                        onFail(failure);
                        Time.Sleep(sleepAfterFail);
                    }
                }
            }

            private Failure CaptureFailure(Exception ex)
            {
                var f = new Failure(ex, DateTime.UtcNow - tryStart, tryNumber);
                failures.Add(f);
                return f;
            }

            private void CheckMaximums()
            {
                if (Duration() > maxTimeout) Fail();
                if (tryNumber > maxRetries) Fail();
            }

            private void Fail()
            {
                throw new TimeoutException($"Retry '{description}' timed out after {tryNumber} tries over {Time.FormatDuration(Duration())}: {GetFailureReport}",
                        new AggregateException(failures.Select(f => f.Exception)));
            }

            private string GetFailureReport()
            {
                return Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => f.Describe()));
            }

            private TimeSpan Duration()
            {
                return DateTime.UtcNow - start;
            }
        }
    }

    public class Failure
    {
        public Failure(Exception exception, TimeSpan duration, int tryNumber)
        {
            Exception = exception;
            Duration = duration;
            TryNumber = tryNumber;
        }

        public Exception Exception { get; }
        public TimeSpan Duration { get; }
        public int TryNumber { get; }

        public string Describe()
        {
            return $"Try {TryNumber} failed after {Time.FormatDuration(Duration)} with exception '{Exception}'";
        }
    }
}
