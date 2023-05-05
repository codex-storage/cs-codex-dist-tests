using NUnit.Framework;
using Utils;

namespace DistTestCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UseLongTimeoutsAttribute : PropertyAttribute
    {
    }

    public interface ITimeSet
    {
        TimeSpan HttpCallTimeout();
        int HttpCallRetryCount();
        void HttpCallRetryDelay();
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

        public void HttpCallRetryDelay()
        {
            Time.Sleep(TimeSpan.FromSeconds(3));
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

        public void HttpCallRetryDelay()
        {
            Time.Sleep(TimeSpan.FromMinutes(5));
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
