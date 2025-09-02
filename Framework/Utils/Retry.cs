namespace Utils
{
    public class Retry
    {
        private readonly string description;
        private readonly TimeSpan maxTimeout;
        private readonly TimeSpan sleepAfterFail;
        private readonly Action<Failure> onFail;
        private readonly bool failFast;

        public Retry(string description, TimeSpan maxTimeout, TimeSpan sleepAfterFail, Action<Failure> onFail, bool failFast)
        {
            this.description = description;
            this.maxTimeout = maxTimeout;
            this.sleepAfterFail = sleepAfterFail;
            this.onFail = onFail;
            this.failFast = failFast;
        }

        public void Run(Action task)
        {
            var run = new RetryRun(description, task, maxTimeout, sleepAfterFail, onFail, failFast);
            run.Run();
        }

        public T Run<T>(Func<T> task)
        {
            T? result = default;

            var run = new RetryRun(description, () =>
            {
                result = task();
            }, maxTimeout, sleepAfterFail, onFail, failFast);
            run.Run();

            return result!;
        }

        private class RetryRun
        {
            private readonly string description;
            private readonly Action task;
            private readonly TimeSpan maxTimeout;
            private readonly TimeSpan sleepAfterFail;
            private readonly Action<Failure> onFail;
            private readonly DateTime start = DateTime.UtcNow;
            private readonly List<Failure> failures = new List<Failure>();
            private readonly bool failFast;
            private int tryNumber;
            private DateTime tryStart;

            public RetryRun(string description, Action task, TimeSpan maxTimeout, TimeSpan sleepAfterFail, Action<Failure> onFail, bool failFast)
            {
                this.description = description;
                this.task = task;
                this.maxTimeout = maxTimeout;
                this.sleepAfterFail = sleepAfterFail;
                this.onFail = onFail;
                this.failFast = failFast;

                tryNumber = 0;
                tryStart = DateTime.UtcNow;
            }

            public void Run()
            {
                while (true)
                {
                    CheckMaximums();

                    tryNumber++;
                    tryStart = DateTime.UtcNow;
                    try
                    {
                        task();
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
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
                if (tryNumber > 30) Fail();

                // If we have a few very fast failures, retrying won't help us. There's probably something wrong with our operation.
                // In this case, don't wait the full duration and fail quickly.
                if (failFast && failures.Count > 5 && failures.All(f => f.Duration < TimeSpan.FromSeconds(1.0))) Fail();
            }

            private void Fail()
            {
                throw new TimeoutException($"Retry '{description}' timed out after {tryNumber} tries over {Time.FormatDuration(Duration())}: {GetFailureReport()}",
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
