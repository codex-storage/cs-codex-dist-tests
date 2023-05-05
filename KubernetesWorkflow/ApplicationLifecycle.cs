using Utils;

namespace KubernetesWorkflow
{
    public class ApplicationLifecycle
    {
        private static object instanceLock = new object();
        private static ApplicationLifecycle? instance;
        private readonly NumberSource servicePortNumberSource = new NumberSource(30001);
        private readonly NumberSource namespaceNumberSource = new NumberSource(0);

        private ApplicationLifecycle()
        {
        }

        public static ApplicationLifecycle Instance
        {
            // I know singletons are quite evil. But we need to be sure this object is created only once
            // and persists for the entire application lifecycle.
            get
            {
                lock (instanceLock)
                {
                    if (instance == null) instance = new ApplicationLifecycle();
                    return instance;
                }
            }
        }

        public NumberSource GetServiceNumberSource()
        {
            return servicePortNumberSource;
        }

        public string GetTestNamespace()
        {
            return namespaceNumberSource.GetNextNumber().ToString("D5");
        }
    }
}
