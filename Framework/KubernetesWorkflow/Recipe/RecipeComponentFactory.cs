using System.Globalization;
using Utils;

namespace KubernetesWorkflow.Recipe
{
    public class RecipeComponentFactory
    {
        private readonly NumberSource internalNumberSource = new NumberSource(8080);
        private static readonly NumberSource externalNumberSource = new NumberSource(30000);
        private static int[] usedExternalPorts = Array.Empty<int>();

        public void Update(K8sController controller)
        {
            usedExternalPorts = controller.GetUsedExternalPorts();
        }

        public Port CreateInternalPort(string tag, PortProtocol protocol)
        {
            return new Port(internalNumberSource.GetNextNumber(), tag, protocol);
        }

        public Port CreateExternalPort(int number, string tag, PortProtocol protocol)
        {
            if (usedExternalPorts.Contains(number)) throw new Exception($"External port number {number} is already in use by the cluster.");
            return new Port(number, tag, protocol);
        }

        public Port CreateExternalPort(string tag, PortProtocol protocol)
        {
            while (true)
            {
                var number = externalNumberSource.GetNextNumber();
                if (!usedExternalPorts.Contains(number))
                {
                    return new Port(number, tag, protocol);
                }
            }
        }

        public EnvVar CreateEnvVar(string name, int value)
        {
            return CreateEnvVar(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public EnvVar CreateEnvVar(string name, string value)
        {
            return new EnvVar(name, value);
        }
    }
}
