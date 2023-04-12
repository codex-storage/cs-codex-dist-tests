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
            var match = configs.Single(c => c.GetType() == typeof(T));
            return (T)match;
        }
    }
}
