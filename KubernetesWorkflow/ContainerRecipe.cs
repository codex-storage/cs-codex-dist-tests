namespace KubernetesWorkflow
{
    public class ContainerRecipe
    {
        public ContainerRecipe(int number, string image, Port[] exposedPorts, Port[] internalPorts, EnvVar[] envVars)
        {
            Number = number;
            Image = image;
            ExposedPorts = exposedPorts;
            InternalPorts = internalPorts;
            EnvVars = envVars;
        }

        public string Name { get { return $"ctnr{Number}"; } }
        public int Number { get; }
        public string Image { get; }
        public Port[] ExposedPorts { get; }
        public Port[] InternalPorts { get; }
        public EnvVar[] EnvVars { get; }

        public Port GetPortByTag(string tag)
        {
            return ExposedPorts.Concat(InternalPorts).Single(p => p.Tag == tag);
        }
    }

    public class Port
    {
        public Port(int number, string tag)
        {
            Number = number;
            Tag = tag;
        }

        public int Number { get; }
        public string Tag { get; }
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
    }
}
