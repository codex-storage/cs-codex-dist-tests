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

            var recipe = new ContainerRecipe(containerNumber, Image, exposedPorts.ToArray(), internalPorts.ToArray(), envVars.ToArray());

            exposedPorts.Clear();
            internalPorts.Clear();
            envVars.Clear();
            this.factory = null!;

            return recipe;
        }

        protected abstract string Image { get; }
        protected int ContainerNumber { get; private set; } = 0;
        protected abstract void Initialize(StartupConfig config);

        protected Port AddExposedPort(string tag = "")
        {
            var p = factory.CreatePort(tag);
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
    }
}
