namespace KubernetesWorkflow.Recipe
{
    public class ContainerRecipe
    {
        public ContainerRecipe(DateTime recipeCreatedUtc, int number, string? nameOverride, string image, ContainerResources resources, SchedulingAffinity schedulingAffinity, CommandOverride commandOverride, bool setCriticalPriority, Port[] exposedPorts, Port[] internalPorts, EnvVar[] envVars, PodLabels podLabels, PodAnnotations podAnnotations, VolumeMount[] volumes, ContainerAdditionals additionals)
        {
            Number = number;
            NameOverride = nameOverride;
            Image = image;
            Resources = resources;
            SchedulingAffinity = schedulingAffinity;
            CommandOverride = commandOverride;
            SetCriticalPriority = setCriticalPriority;
            ExposedPorts = exposedPorts;
            InternalPorts = internalPorts;
            EnvVars = envVars;
            PodLabels = podLabels;
            PodAnnotations = podAnnotations;
            Volumes = volumes;
            Additionals = additionals;

            if (NameOverride != null)
            {
                Name = $"{K8sNameUtils.Format(NameOverride)}-{Number}";
            }
            else
            {
                Name = $"ctnr{Number}";
            }

            if (exposedPorts.Any(p => string.IsNullOrEmpty(p.Tag))) throw new Exception("Port tags are required for all exposed ports.");
        }

        public DateTime RecipeCreatedUtc { get; }
        public string Name { get; }
        public int Number { get; }
        public string? NameOverride { get; }
        public ContainerResources Resources { get; }
        public SchedulingAffinity SchedulingAffinity { get; }
        public CommandOverride CommandOverride { get; }
        public bool SetCriticalPriority { get; }
        public string Image { get; }
        public Port[] ExposedPorts { get; }
        public Port[] InternalPorts { get; }
        public EnvVar[] EnvVars { get; }
        public PodLabels PodLabels { get; }
        public PodAnnotations PodAnnotations { get; }
        public VolumeMount[] Volumes { get; }
        public ContainerAdditionals Additionals { get; }

        public Port? GetPortByTag(string tag)
        {
            return ExposedPorts.Concat(InternalPorts).SingleOrDefault(p => p.Tag == tag);
        }

        public override string ToString()
        {
            return $"(container-recipe: {Name}, image: {Image}, " +
                $"exposedPorts: {string.Join(",", ExposedPorts.Select(p => p.Number))}, " +
                $"internalPorts: {string.Join(",", InternalPorts.Select(p => p.Number))}, " +
                $"envVars: {string.Join(",", EnvVars.Select(v => v.ToString()))}, " +
                $"limits: {Resources}, " +
                $"affinity: {SchedulingAffinity}, " +
                $"volumes: {string.Join(",", Volumes.Select(v => $"'{v.MountPath}'"))}";
        }
    }

    public class Port
    {
        public Port(int number, string tag, PortProtocol protocol)
        {
            Number = number;
            Tag = tag;
            Protocol = protocol;

            if (string.IsNullOrWhiteSpace(Tag))
            {
                throw new Exception("A unique port tag is required");
            }
        }

        public int Number { get; }
        public string Tag { get; }
        public PortProtocol Protocol { get; }

        public bool IsTcp()
        {
            return Protocol == PortProtocol.TCP;
        }

        public bool IsUdp()
        {
            return Protocol == PortProtocol.UDP;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Tag)) return $"untagged-port={Number}/{Protocol}";
            return $"{Tag}={Number}/{Protocol}";
        }
    }

    public enum PortProtocol
    {
        TCP,
        UDP
    }

    public class EnvVar
    {
        public EnvVar(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }

        public override string ToString()
        {
            return $"'{Name}' = '{Value}'";
        }
    }

    public class VolumeMount
    {
        public VolumeMount(string volumeName, string mountPath, string? subPath = null, string? resourceQuantity = null, string? secret = null, string? hostPath = null)
        {
            VolumeName = volumeName;
            MountPath = mountPath;
            SubPath = subPath;
            ResourceQuantity = resourceQuantity;
            Secret = secret;
            HostPath = hostPath;
        }

        public string VolumeName { get; }
        public string MountPath { get; }
        public string? SubPath { get; }
        public string? ResourceQuantity { get; }
        public string? Secret { get; }
        public string? HostPath { get; }
    }
}
