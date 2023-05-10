namespace Utils
{
    public static class Time
    {
        public static void Sleep(TimeSpan span)
        {
            Thread.Sleep(span);
        }

        public static T Wait<T>(Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static string FormatDuration(TimeSpan d)
        {
            var result = "";
            if (d.Days > 0) result += $"{d.Days} days, ";
            if (d.Hours > 0) result += $"{d.Hours} hours, ";
            if (d.Minutes > 0) result += $"{d.Minutes} mins, ";
            result += $"{d.Seconds} secs";
            return result;
        }

        public static void WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan retryTime)
        {
            var start = DateTime.UtcNow;
            var state = predicate();
            while (!state)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Operation timed out.");
                }

                Sleep(retryTime);
                state = predicate();
            }
        }

        public static void Retry(Action action)
        {
            Retry(action, TimeSpan.FromMinutes(1));
        }

        public static T Retry<T>(Func<T> action)
        {
            return Retry(action, TimeSpan.FromMinutes(1));
        }

        public static void Retry(Action action, TimeSpan timeout)
        {
            Retry(action, timeout, TimeSpan.FromSeconds(1));
        }

        public static T Retry<T>(Func<T> action, TimeSpan timeout)
        {
            return Retry(action, timeout, TimeSpan.FromSeconds(1));
        }

        public static void Retry(Action action, TimeSpan timeout, TimeSpan retryTime)
        {
            var start = DateTime.UtcNow;
            var exceptions = new List<Exception>();
            while (true)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Retry timed out.", new AggregateException(exceptions));
                }

                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                Sleep(retryTime);
            }
        }

        public static T Retry<T>(Func<T> action, TimeSpan timeout, TimeSpan retryTime)
        {
            var start = DateTime.UtcNow;
            var exceptions = new List<Exception>();
            while (true)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Retry timed out.", new AggregateException(exceptions));
                }

                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                Sleep(retryTime);
            }
        }
    }
}
