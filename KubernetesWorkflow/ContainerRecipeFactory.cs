namespace KubernetesWorkflow
{
    public abstract class ContainerRecipeFactory
    {
        private readonly List<Port> exposedPorts = new List<Port>();
        private readonly List<Port> internalPorts = new List<Port>();
        private readonly List<EnvVar> envVars = new List<EnvVar>();
        private RecipeComponentFactory factory = null!;

        public ContainerRecipe CreateRecipe(int containerNumber, RecipeComponentFactory factory, StartupConfig config)
        {
            this.factory = factory;
            ContainerNumber = containerNumber;

            Initialize(config);

            return new ContainerRecipe(containerNumber, Image, exposedPorts.ToArray(), internalPorts.ToArray(), envVars.ToArray());
        }

        protected abstract string Image { get; }
        protected int ContainerNumber { get; private set; } = 0;
        protected abstract void Initialize(StartupConfig config);

        protected Port AddExposedPort()
        {
            var p = factory.CreatePort();
            exposedPorts.Add(p);
            return p;
        }

        protected Port AddInternalPort()
        {
            var p = factory.CreatePort();
            internalPorts.Add(p);
            return p;
        }

        protected void AddExposedPortAndVar(string name)
        {
            AddEnvVar(name, AddExposedPort());
        }

        protected void AddInternalPortAndVar(string name)
        {
            AddEnvVar(name, AddInternalPort());
        }

        protected void AddEnvVar(string name, string value)
        {
            envVars.Add(factory.CreateEnvVar(name, value));
        }

        protected void AddEnvVar(string name, Port value)
        {
            envVars.Add(factory.CreateEnvVar(name, value.Number));
        }
    }
}
