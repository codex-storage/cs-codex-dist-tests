using Core;

namespace DistTestCore
{
    public class LongTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromHours(2);
        }

        public TimeSpan HttpCallRetryTime()
        {
            return TimeSpan.FromHours(5);
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(2);
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
