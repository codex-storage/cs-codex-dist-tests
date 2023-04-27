using NUnit.Framework;
using Utils;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UseLongTimeoutsAttribute : PropertyAttribute
    {
    }

    public static class Timing
    {
        public static bool UseLongTimeouts { get; set; }


        public static TimeSpan HttpCallTimeout()
        {
            return GetTimes().HttpCallTimeout();
        }

        public static int HttpCallRetryCount()
        {
            return GetTimes().HttpCallRetryCount();
        }

        public static void HttpCallRetryDelay()
        {
            Time.Sleep(GetTimes().HttpCallRetryDelay());
        }

        public static TimeSpan K8sServiceDelay()
        {
            return GetTimes().WaitForK8sServiceDelay();
        }

        public static TimeSpan K8sOperationTimeout()
        {
            return GetTimes().K8sOperationTimeout();
        }

        public static TimeSpan WaitForMetricTimeout()
        {
            return GetTimes().WaitForMetricTimeout();
        }

        private static ITimeSet GetTimes()
        {
            if (UseLongTimeouts) return new LongTimeSet();
            return new DefaultTimeSet();
        }
    }

    public interface ITimeSet
    {
        TimeSpan HttpCallTimeout();
        int HttpCallRetryCount();
        TimeSpan HttpCallRetryDelay();
        TimeSpan WaitForK8sServiceDelay();
        TimeSpan K8sOperationTimeout();
        TimeSpan WaitForMetricTimeout();
    }

    public class DefaultTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromSeconds(10);
        }

        public int HttpCallRetryCount()
        {
            return 5;
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(3);
        }

        public TimeSpan WaitForK8sServiceDelay()
        {
            return TimeSpan.FromSeconds(1);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(5);
        }

        public TimeSpan WaitForMetricTimeout()
        {
            return TimeSpan.FromSeconds(30);
        }
    }

    public class LongTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromHours(2);
        }

        public int HttpCallRetryCount()
        {
            return 2;
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromMinutes(5);
        }

        public TimeSpan WaitForK8sServiceDelay()
        {
            return TimeSpan.FromSeconds(10);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(15);
        }

        public TimeSpan WaitForMetricTimeout()
        {
            return TimeSpan.FromMinutes(5);
        }
    }
}
