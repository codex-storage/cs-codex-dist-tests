namespace AutoClient.Modes.FolderStore
{
    public class SlowModeHandler : IFileSaverResultHandler
    {
        private readonly App app;
        private int failureCount = 0;
        private bool slowMode = false;
        private int recoveryCount = 0;

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
                if (recoveryCount > 2)
                {
                    Log("Recovery limit reached. Exiting slow mode.");
                    slowMode = false;
                    failureCount = 0;
                }
            }
        }

        public void OnFailure()
        {
            failureCount++;
            if (failureCount > 5 && !slowMode)
            {
                Log("Failure limit reached. Entering slow mode.");
                slowMode = true;
                recoveryCount = 0;
            }
        }

        public void Check()
        {
            if (slowMode)
            {
                Thread.Sleep(TimeSpan.FromMinutes(app.Config.SlowModeDelayMinutes));
            }
        }

        private void Log(string msg)
        {
            app.Log.Log(msg);
        }
    }
}
