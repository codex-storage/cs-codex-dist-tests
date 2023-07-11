namespace ContinuousTests
{
    public class TaskFactory
    {
        private readonly object taskLock = new();
        private readonly List<Task> activeTasks = new List<Task>();

        public void Run(Action action)
        {
            lock (taskLock)
            {
                activeTasks.Add(Task.Run(action).ContinueWith(CleanupTask, null));
            }
        }

        public void WaitAll()
        {
            var tasks = activeTasks.ToArray();
            Task.WaitAll(tasks);

            var moreTasks = false;
            lock (taskLock)
            {
                activeTasks.RemoveAll(task => task.IsCompleted);
                moreTasks = activeTasks.Any();
            }

            if (moreTasks) WaitAll();
        }

        private void CleanupTask(Task completedTask, object? arg)
        {
            lock (taskLock)
            {
                activeTasks.Remove(completedTask);
            }
        }
    }
}
