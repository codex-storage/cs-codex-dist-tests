using NUnit.Framework;

namespace CodexDistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UseLongTimeoutsAttribute : PropertyAttribute
    {
        public UseLongTimeoutsAttribute()
            : base(Timing.UseLongTimeoutsKey)
        {
        }
    }

    public static class Timing
    {
        public const string UseLongTimeoutsKey = "UseLongTimeouts";

        public static TimeSpan HttpCallTimeout()
        {
            return GetTimes().HttpCallTimeout();
        }

        public static int HttpCallRetryCount()
        {
            return GetTimes().HttpCallRetryCount();
        }

        public static void RetryDelay()
        {
            Utils.Sleep(GetTimes().RetryDelay());
        }

        public static void WaitForK8sServiceDelay()
        {
            Utils.Sleep(GetTimes().WaitForK8sServiceDelay());
        }

        public static TimeSpan K8sOperationTimeout()
        {
            return GetTimes().K8sOperationTimeout();
        }

        private static ITimeSet GetTimes()
        {
            var testProperties = TestContext.CurrentContext.Test.Properties;
            if (testProperties.ContainsKey(UseLongTimeoutsKey)) return new LongTimeSet();
            return new DefaultTimeSet();
        }
    }

    public interface ITimeSet
    {
        TimeSpan HttpCallTimeout();
        int HttpCallRetryCount();
        TimeSpan RetryDelay();
        TimeSpan WaitForK8sServiceDelay();
        TimeSpan K8sOperationTimeout();
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

        public TimeSpan RetryDelay()
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

        public TimeSpan RetryDelay()
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
    }
}
