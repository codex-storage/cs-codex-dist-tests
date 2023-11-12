using Utils;

namespace KubernetesWorkflow.Types
{
    public class ContainerAddress
    {
        public ContainerAddress(string portTag, Address address, bool isInteral)
        {
            PortTag = portTag;
            Address = address;
            IsInteral = isInteral;
        }

        public string PortTag { get; }
        public Address Address { get; }
        public bool IsInteral { get; }

        public override string ToString()
        {
            var indicator = IsInteral ? "int" : "ext";
            return $"{indicator} {PortTag} -> '{Address}'";
        }
    }
}
