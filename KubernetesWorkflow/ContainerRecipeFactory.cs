namespace KubernetesWorkflow
{
    public abstract class ContainerRecipeFactory
    {
        private readonly List<Port> exposedPorts = new List<Port>();
        private readonly List<Port> internalPorts = new List<Port>();
        private readonly List<EnvVar> envVars = new List<EnvVar>();
        private readonly PodLabels podLabels = new PodLabels();
        private readonly PodAnnotations podAnnotations = new PodAnnotations();
        private readonly List<VolumeMount> volumeMounts = new List<VolumeMount>();
        private readonly List<object> additionals = new List<object>();
        private RecipeComponentFactory factory = null!;

        public ContainerRecipe CreateRecipe(int index, int containerNumber, RecipeComponentFactory factory, StartupConfig config)
        {
            this.factory = factory;
            ContainerNumber = containerNumber;
            Index = index;

            Initialize(config);

            var recipe = new ContainerRecipe(containerNumber, Image,
                exposedPorts.ToArray(),
                internalPorts.ToArray(),
                envVars.ToArray(),
                podLabels.Clone(),
                podAnnotations.Clone(),
                volumeMounts.ToArray(),
                additionals.ToArray());

            exposedPorts.Clear();
            internalPorts.Clear();
            envVars.Clear();
            podLabels.Clear();
            podAnnotations.Clear();
            volumeMounts.Clear();
            additionals.Clear();
            this.factory = null!;

            return recipe;
        }

        public abstract string AppName { get; }
        public abstract string Image { get; }
        protected int ContainerNumber { get; private set; } = 0;
        protected int Index { get; private set; } = 0;
        protected abstract void Initialize(StartupConfig config);

        protected Port AddExposedPort(string tag = "")
        {
            var p = factory.CreatePort(tag);
            exposedPorts.Add(p);
            return p;
        }

        protected Port AddExposedPort(int number, string tag = "")
        {
            var p = factory.CreatePort(number, tag);
            exposedPorts.Add(p);
            return p;
        }

        protected Port AddInternalPort(string tag = "")
        {
            var p = factory.CreatePort(tag);
            internalPorts.Add(p);
            return p;
        }

        protected void AddExposedPortAndVar(string name, string tag = "")
        {
            AddEnvVar(name, AddExposedPort(tag));
        }

        protected void AddInternalPortAndVar(string name, string tag = "")
        {
            AddEnvVar(name, AddInternalPort(tag));
        }

        protected void AddEnvVar(string name, string value)
        {
            envVars.Add(factory.CreateEnvVar(name, value));
        }

        protected void AddEnvVar(string name, Port value)
        {
            envVars.Add(factory.CreateEnvVar(name, value.Number));
        }

        protected void AddPodLabel(string name, string value)
        {
            podLabels.Add(name, value);
        }

        protected void AddPodAnnotation(string name, string value)
        {
            podAnnotations.Add(name, value);
        }

        protected void AddVolume(string mountPath)
        {
            volumeMounts.Add(new VolumeMount(
                $"autovolume-{Guid.NewGuid().ToString().ToLowerInvariant()}",
                mountPath));
        }

        protected void Additional(object userData)
        {
            additionals.Add(userData);
        }
    }
}
