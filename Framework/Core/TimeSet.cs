namespace Core
{
    public interface ITimeSet
    {
        /// <summary>
        /// Timeout for a single HTTP call.
        /// </summary>
        TimeSpan HttpCallTimeout();

        /// <summary>
        /// Maximum total time to attempt to make a successful HTTP call to a service.
        /// When HTTP calls time out during this timespan, retries will be made.
        /// </summary>
        TimeSpan HttpRetryTimeout();

        /// <summary>
        /// After a failed HTTP call, wait this long before trying again.
        /// </summary>
        TimeSpan HttpCallRetryDelay();

        /// <summary>
        /// After a failed K8s operation, wait this long before trying again.
        /// </summary>
        TimeSpan K8sOperationRetryDelay();

        /// <summary>
        /// Maximum total time to attempt to perform a successful k8s operation.
        /// If k8s operations fail during this timespan, retries will be made.
        /// </summary>
        TimeSpan K8sOperationTimeout();
    }

    public class DefaultTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(2);
        }

        public TimeSpan HttpRetryTimeout()
        {
            return TimeSpan.FromMinutes(5);
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(1);
        }

        public TimeSpan K8sOperationRetryDelay()
        {
            return TimeSpan.FromSeconds(10);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(30);
        }
    }

    public class LongTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(30);
        }

        public TimeSpan HttpRetryTimeout()
        {
            return TimeSpan.FromHours(2.2);
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(20);
        }

        public TimeSpan K8sOperationRetryDelay()
        {
            return TimeSpan.FromSeconds(30);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromHours(1);
        }
    }
}
