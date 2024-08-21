
namespace MarketInsights
{
    public class Updater
    {
        private readonly AppState appState;
        private readonly Tracker[] trackers;

        public Updater(AppState appState)
        {
            this.appState = appState;
            trackers = CreateTrackers();
        }

        private Tracker[] CreateTrackers()
        {
            var tokens = appState.Config.TimeSegments.Split(";", StringSplitOptions.RemoveEmptyEntries);
            var nums = tokens.Select(t => Convert.ToInt32(t)).ToArray();
            return nums.Select(n => new Tracker(n)).ToArray();
        }

        public void Run()
        {

        }
    }

    public class Tracker
    {
        public Tracker(int numberOfSegments)
        {
            
        }
    }
}
