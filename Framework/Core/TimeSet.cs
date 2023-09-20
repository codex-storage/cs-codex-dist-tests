namespace Core
{
    public interface ITimeSet
    {
        TimeSpan HttpCallTimeout();
        TimeSpan HttpCallRetryTime();
        TimeSpan HttpCallRetryDelay();
        TimeSpan WaitForK8sServiceDelay();
        TimeSpan K8sOperationTimeout();
    }

    public class DefaultTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(5);
        }

        public TimeSpan HttpCallRetryTime()
        {
            return TimeSpan.FromMinutes(1);
        }

        public TimeSpan HttpCallRetryDelay()
        {
            return TimeSpan.FromSeconds(1);
        }

        public TimeSpan WaitForK8sServiceDelay()
        {
            return TimeSpan.FromSeconds(10);
        }

        public TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(30);
        }
    }
}
