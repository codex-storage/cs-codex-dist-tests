namespace AutoClient.Modes.FolderStore
{
    public class SlowModeHandler : IFileSaverResultHandler
    {
        private readonly App app;
        private int failureCount = 0;
        private bool slowMode = false;
        private int recoveryCount = 0;
        private readonly object _lock = new object();

        public SlowModeHandler(App app)
        {
            this.app = app;
        }

        public void OnSuccess()
        {
            failureCount = 0;
            if (slowMode)
            {
                recoveryCount++;
                Log("Recovering from slow mode: " + recoveryCount);
                if (recoveryCount > 3)
                {
                    Log("Recovery limit reached. Exiting slow mode.");
                    slowMode = false;
                    failureCount = 0;
                }
            }

            Check();
        }

        public void OnFailure()
        {
            failureCount++;
            Log("Failing towards slow mode: " + failureCount);
            if (failureCount > 3 && !slowMode)
            {
                Log("Failure limit reached. Entering slow mode.");
                slowMode = true;
                recoveryCount = 0;
            }

            Check();
        }

        private void Check()
        {
            if (slowMode)
            {
                lock (_lock)
                {
                    if (!slowMode) return;
                    Thread.Sleep(TimeSpan.FromMinutes(app.Config.SlowModeDelayMinutes));
                }
            }
        }

        private void Log(string msg)
        {
            app.Log.Log(msg);
        }
    }
}
