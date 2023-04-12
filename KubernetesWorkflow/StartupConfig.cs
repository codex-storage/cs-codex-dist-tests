namespace KubernetesWorkflow
{
    public class StartupConfig
    {
        private readonly List<object> configs = new List<object>();

        public void Add(object config)
        {
            configs.Add(config);
        }

        public T Get<T>()
        {
            var match = configs.Single(c => typeof(T).IsAssignableFrom(c.GetType()));
            return (T)match;
        }
    }
}
