namespace CodexDistTests.TestCore
{
    public static class Timing
    {
        public static TimeSpan HttpCallTimeout()
        {
            return TimeSpan.FromSeconds(10);
        }

        public static int HttpCallRetryCount()
        {
            return 5;
        }

        public static void RetryDelay()
        {
            Utils.Sleep(TimeSpan.FromSeconds(3));
        }

        public static void WaitForK8sServiceDelay()
        {
            Utils.Sleep(TimeSpan.FromSeconds(1));
        }

        public static TimeSpan K8sOperationTimeout()
        {
            return TimeSpan.FromMinutes(5);
        }
    }
}
