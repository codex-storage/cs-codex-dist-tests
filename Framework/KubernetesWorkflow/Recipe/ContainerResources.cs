using Utils;

namespace KubernetesWorkflow.Recipe
{
    public class ContainerResources
    {
        public ContainerResourceSet Requests { get; set; } = new ContainerResourceSet();
        public ContainerResourceSet Limits { get; set; } = new ContainerResourceSet();

        public override string ToString()
        {
            return $"requests:{Requests}, limits:{Limits}";
        }
    }

    public class ContainerResourceSet
    {
        public ContainerResourceSet(int milliCPUs, ByteSize memory)
        {
            MilliCPUs = milliCPUs;
            Memory = memory;
        }

        public ContainerResourceSet(int milliCPUs)
            : this(milliCPUs, new ByteSize(0))
        {
        }

        public ContainerResourceSet(ByteSize memory)
            : this(0, memory)
        {
        }

        public ContainerResourceSet()
            : this(0)
        {
        }

        public int MilliCPUs { get; }
        public ByteSize Memory { get; }

        public override string ToString()
        {
            var result = new List<string>();
            if (MilliCPUs == 0) result.Add("cpu: unlimited");
            else result.Add($"cpu: {MilliCPUs} milliCPUs");
            if (Memory.SizeInBytes == 0) result.Add("memory: unlimited");
            else result.Add($"memory: {Memory}");
            return string.Join(", ", result);
        }
    }
}
