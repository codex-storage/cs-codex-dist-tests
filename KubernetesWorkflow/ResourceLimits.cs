using Utils;

namespace KubernetesWorkflow
{
    public class ResourceLimits
    {
        public ResourceLimits(int milliCPUs, ByteSize memory)
        {
            MilliCPUs = milliCPUs;
            Memory = memory;
        }

        public ResourceLimits(int milliCPUs)
            : this(milliCPUs, new ByteSize(0))
        {
        }

        public ResourceLimits(ByteSize memory)
            : this(0, memory)
        {
        }

        public ResourceLimits()
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
