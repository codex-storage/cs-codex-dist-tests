namespace Utils
{
    public class TaskFactory
    {
        private readonly object taskLock = new();
        private readonly List<Task> activeTasks = new List<Task>();

        public void Run(Action action, string name)
        {
            lock (taskLock)
            {
                activeTasks.Add(Task.Run(() => CatchException(action, name)).ContinueWith(CleanupTask, null));
            }
        }

        private void CatchException(Action action, string name)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in task '{name}': " + ex);
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
