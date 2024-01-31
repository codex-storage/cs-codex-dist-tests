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

        public static void Wait(Task task)
        {
            task.Wait();
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
        
        public static TimeSpan ParseTimespan(string span)
        {
            span = span.Replace(" ", "").Replace(",", "");
            var result = TimeSpan.Zero;
            var number = "";
            foreach (var c in span)
            {
                if (char.IsNumber(c)) number += c;
                else
                {
                    var value = Convert.ToInt32(number);
                    number = "";

                    if (c == 'd') result += TimeSpan.FromDays(value);
                    else if (c == 'h') result += TimeSpan.FromHours(value);
                    else if (c == 'm') result += TimeSpan.FromMinutes(value);
                    else if (c == 's') result += TimeSpan.FromSeconds(value);
                    else throw new Exception("Unknown time modifier: " + c);
                }
            }

            if (!string.IsNullOrEmpty(number))
            {
                var value = Convert.ToInt32(number);
                result += TimeSpan.FromSeconds(value);
            }
            return result;
        }

        public static void WaitUntil(Func<bool> predicate)
        {
            WaitUntil(predicate, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1));
        }

        public static void WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan retryDelay)
        {
            var start = DateTime.UtcNow;
            var state = predicate();
            while (!state)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Operation timed out.");
                }

                Sleep(retryDelay);
                state = predicate();
            }
        }

        public static void Retry(Action action, string description)
        {
            Retry(action, 1, description);
        }

        public static T Retry<T>(Func<T> action, string description)
        {
            return Retry(action, 1, description);
        }

        public static void Retry(Action action, int maxRetries, string description)
        {
            Retry(action, maxRetries, TimeSpan.FromSeconds(5), description);
        }

        public static T Retry<T>(Func<T> action, int maxRetries, string description)
        {
            return Retry(action, maxRetries, TimeSpan.FromSeconds(5), description);
        }

        public static void Retry(Action action, int maxRetries, TimeSpan retryTime, string description)
        {
            var start = DateTime.UtcNow;
            var retries = 0;
            var exceptions = new List<Exception>();
            while (true)
            {
                if (retries > maxRetries)
                {
                    var duration = DateTime.UtcNow - start;
                    throw new TimeoutException($"Retry '{description}' timed out after {maxRetries} tries over {Time.FormatDuration(duration)}.", new AggregateException(exceptions));
                }

                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    retries++;
                }

                Sleep(retryTime);
            }
        }

        public static T Retry<T>(Func<T> action, int maxRetries, TimeSpan retryTime, string description)
        {
            var start = DateTime.UtcNow;
            var retries = 0;
            var exceptions = new List<Exception>();
            while (true)
            {
                if (retries > maxRetries)
                {
                    var duration = DateTime.UtcNow - start;
                    throw new TimeoutException($"Retry '{description}' timed out after {maxRetries} tries over {Time.FormatDuration(duration)}.", new AggregateException(exceptions));
                }

                try
                {
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    retries++;
                }

                Sleep(retryTime);
            }
        }
    }
}
