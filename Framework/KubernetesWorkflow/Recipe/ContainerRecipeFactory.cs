using Utils;

namespace KubernetesWorkflow.Recipe
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
        private ContainerResources resources = new ContainerResources();
        private SchedulingAffinity schedulingAffinity = new SchedulingAffinity();
        private bool setCriticalPriority;

        public ContainerRecipe CreateRecipe(int index, int containerNumber, RecipeComponentFactory factory, StartupConfig config)
        {
            this.factory = factory;
            ContainerNumber = containerNumber;
            Index = index;

            Initialize(config);

            var recipe = new ContainerRecipe(containerNumber, config.NameOverride, Image, resources, schedulingAffinity, setCriticalPriority,
                exposedPorts.ToArray(),
                internalPorts.ToArray(),
                envVars.ToArray(),
                podLabels.Clone(),
                podAnnotations.Clone(),
                volumeMounts.ToArray(),
                ContainerAdditionals.CreateFromUserData(additionals));

            exposedPorts.Clear();
            internalPorts.Clear();
            envVars.Clear();
            podLabels.Clear();
            podAnnotations.Clear();
            volumeMounts.Clear();
            additionals.Clear();
            this.factory = null!;
            resources = new ContainerResources();
            schedulingAffinity = new SchedulingAffinity();
            setCriticalPriority = false;

            return recipe;
        }

        public abstract string AppName { get; }
        public abstract string Image { get; }
        protected int ContainerNumber { get; private set; } = 0;
        protected int Index { get; private set; } = 0;
        protected abstract void Initialize(StartupConfig config);

        protected Port AddExposedPort(string tag, PortProtocol protocol = PortProtocol.TCP)
        {
            return AddExposedPort(factory.CreatePort(tag, protocol));
        }

        protected Port AddExposedPort(int number, string tag, PortProtocol protocol = PortProtocol.TCP)
        {
            return AddExposedPort(factory.CreatePort(number, tag, protocol));
        }

        protected Port AddInternalPort(string tag = "", PortProtocol protocol = PortProtocol.TCP)
        {
            var p = factory.CreatePort(tag, protocol);
            internalPorts.Add(p);
            return p;
        }

        protected void AddExposedPortAndVar(string name, string tag, PortProtocol protocol = PortProtocol.TCP)
        {
            AddEnvVar(name, AddExposedPort(tag, protocol));
        }

        protected void AddInternalPortAndVar(string name, string tag = "", PortProtocol protocol = PortProtocol.TCP)
        {
            AddEnvVar(name, AddInternalPort(tag, protocol));
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

        protected void AddVolume(string name, string mountPath, string? subPath = null, string? secret = null, string? hostPath = null)
        {
            var size = 10.MB().ToSuffixNotation();
            volumeMounts.Add(new VolumeMount(name, mountPath, subPath, size, secret, hostPath));
        }

        protected void AddVolume(string mountPath, ByteSize volumeSize)
        {
            volumeMounts.Add(new VolumeMount(
                $"autovolume-{Guid.NewGuid().ToString().ToLowerInvariant()}",
                mountPath,
                resourceQuantity: volumeSize.ToSuffixNotation()));
        }

        protected void Additional(object userData)
        {
            additionals.Add(userData);
        }

        protected void SetResourcesRequest(int milliCPUs, ByteSize memory)
        {
            SetResourcesRequest(new ContainerResourceSet(milliCPUs, memory));
        }

        protected void SetSchedulingAffinity(string notIn)
        {
            schedulingAffinity = new SchedulingAffinity(notIn);
        }

        protected void SetSystemCriticalPriority()
        {
            setCriticalPriority = true;
        }

        // Disabled following a possible bug in the k8s cluster that will throttle containers much more than is
        // called for if they have resource limits defined.
        //protected void SetResourceLimits(int milliCPUs, ByteSize memory)
        //{
        //    SetResourceLimits(new ContainerResourceSet(milliCPUs, memory));
        //}

        protected void SetResourcesRequest(ContainerResourceSet requests)
        {
            resources.Requests = requests;
        }

        protected void SetResourceLimits(ContainerResourceSet limits)
        {
            resources.Limits = limits;
        }

        private Port AddExposedPort(Port port)
        {
            exposedPorts.Add(port);
            return port;
        }
    }
}
