namespace Core
{
    public interface IK8sTimeSet
    {
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

    public class DefaultK8sTimeSet : IK8sTimeSet
    {
        public TimeSpan K8sOperationRetryDelay()
        {
            return TimeSpan.FromSeconds(10);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(30);
        }
    }

    public class LongK8sTimeSet : IK8sTimeSet
    {
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
