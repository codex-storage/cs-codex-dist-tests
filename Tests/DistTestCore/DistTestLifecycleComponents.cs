namespace DistTestCore
{
    public interface ILifecycleComponent
    {
        void Start(ILifecycleComponentAccess access);
        void Stop(ILifecycleComponentAccess access, DistTestResult result);
    }

    public interface ILifecycleComponentCollector
    {
        void AddComponent(ILifecycleComponent component);
    }

    public interface ILifecycleComponentAccess
    {
        T Get<T>() where T : ILifecycleComponent;
    }

    public class DistTestLifecycleComponents
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Dictionary<Type, ILifecycleComponent>> components = new();

        public void Setup(string testName, Action<ILifecycleComponentCollector> initializer)
        {
            var newComponents = new Dictionary<Type, ILifecycleComponent>();
            lock (_lock)
            {
                components.Add(testName, newComponents);
                var collector = new Collector(newComponents);
                initializer(collector);
            }

            var access = new ScopedAccess(this, testName);
            foreach (var component in newComponents.Values)
            {
                component.Start(access);
            }
        }

        public T Get<T>(string testName) where T : ILifecycleComponent
        {
            var type = typeof(T);
            lock (_lock)
            {
                return (T)components[testName][type];
            }
        }

        public void TearDown(string testName, DistTestResult result)
        {
            var access = new ScopedAccess(this, testName);
            var closingComponents = components[testName];
            foreach (var component in closingComponents.Values)
            {
                component.Stop(access, result);
            }

            lock (_lock)
            {
                components.Remove(testName);
            }
        }

        private class Collector : ILifecycleComponentCollector
        {
            private readonly Dictionary<Type, ILifecycleComponent> components;

            public Collector(Dictionary<Type, ILifecycleComponent> components)
            {
                this.components = components;
            }

            public void AddComponent(ILifecycleComponent component)
            {
                components.Add(component.GetType(), component);
            }
        }

        private class ScopedAccess : ILifecycleComponentAccess
        {
            private readonly DistTestLifecycleComponents parent;
            private readonly string testName;

            public ScopedAccess(DistTestLifecycleComponents parent, string testName)
            {
                this.parent = parent;
                this.testName = testName;
            }

            public T Get<T>() where T : ILifecycleComponent
            {
                return parent.Get<T>(testName);
            }
        }
    }
}
