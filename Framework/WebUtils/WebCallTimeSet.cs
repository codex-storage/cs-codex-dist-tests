namespace WebUtils
{
    public interface IWebCallTimeSet
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
    }

    public class DefaultWebCallTimeSet : IWebCallTimeSet
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
    }

    public class LongWebCallTimeSet : IWebCallTimeSet
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
    }
}
