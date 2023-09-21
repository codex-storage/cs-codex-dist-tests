namespace ArgsUniform
{
    public class UniformAttribute : Attribute
    {
        public UniformAttribute(string arg, string argShort, string envVar, bool required, string description)
        {
            Arg = arg;
            ArgShort = argShort;
            EnvVar = envVar;
            Required = required;
            Description = description;
        }

        public string Arg { get; }
        public string ArgShort { get; }
        public string EnvVar { get; }
        public bool Required { get; }
        public string Description { get; }
    }
}
