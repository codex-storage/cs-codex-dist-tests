﻿namespace Utils
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

        public static string FormatDuration(TimeSpan? d)
        {
            if (d == null) return "[NULL]";
            return FormatDuration(d.Value);
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

        public static string FormatTimestamp(DateTime d)
        {
            return d.ToString("o");
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

        public static void WaitUntil(Func<bool> predicate, string msg)
        {
            WaitUntil(predicate, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1), msg);
        }

        public static void WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan retryDelay, string msg)
        {
            var start = DateTime.UtcNow;
            var tries = 1;
            var state = predicate();
            while (!state)
            {
                var duration = DateTime.UtcNow - start;
                if (duration > timeout)
                {
                    throw new TimeoutException($"Operation timed out after {tries} tries over (total) {FormatDuration(duration)}. '{msg}'");
                }

                Sleep(retryDelay);
                state = predicate();
                tries++;
            }
        }

        public static void Retry(Action action, string description)
        {
            Retry(action, TimeSpan.FromSeconds(30), description);
        }

        public static T Retry<T>(Func<T> action, string description)
        {
            return Retry(action, TimeSpan.FromSeconds(30), description);
        }

        public static void Retry(Action action, TimeSpan maxTimeout, string description)
        {
            Retry(action, maxTimeout, TimeSpan.FromSeconds(5), description);
        }

        public static T Retry<T>(Func<T> action, TimeSpan maxTimeout, string description)
        {
            return Retry(action, maxTimeout, TimeSpan.FromSeconds(5), description);
        }

        public static void Retry(Action action, TimeSpan maxTimeout, TimeSpan retryTime, string description)
        {
            Retry(action, maxTimeout, retryTime, description, f => { });
        }

        public static T Retry<T>(Func<T> action, TimeSpan maxTimeout, TimeSpan retryTime, string description)
        {
            return Retry(action, maxTimeout, retryTime, description, f => { });
        }

        public static void Retry(Action action, TimeSpan maxTimeout, TimeSpan retryTime, string description, Action<Failure> onFail)
        {
            var r = new Retry(description, maxTimeout, retryTime, onFail, failFast: true);
            r.Run(action);
        }

        public static T Retry<T>(Func<T> action, TimeSpan maxTimeout, TimeSpan retryTime, string description, Action<Failure> onFail)
        {
            var r = new Retry(description, maxTimeout, retryTime, onFail, failFast: true);
            return r.Run(action);
        }
    }
}
