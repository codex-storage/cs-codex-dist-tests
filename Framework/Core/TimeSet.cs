﻿namespace Core
{
    public interface ITimeSet
    {
        TimeSpan HttpCallTimeout();
        int HttpMaxNumberOfRetries();
        TimeSpan HttpCallRetryDelay();
        TimeSpan WaitForK8sServiceDelay();
        TimeSpan K8sOperationTimeout();
    }

    public class DefaultTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromMinutes(3);
        }

        public int HttpMaxNumberOfRetries()
        {
            return 3;
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

    public class LongTimeSet : ITimeSet
    {
        public TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromHours(2);
        }

        public int HttpMaxNumberOfRetries()
        {
            return 1;
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
    }
}
