using System.Globalization;
using Utils;

namespace KubernetesWorkflow
{
    public class RecipeComponentFactory
    {
        private NumberSource portNumberSource = new NumberSource(8080);

        public Port CreatePort(int number, string tag, PortProtocol protocol)
        {
            return new Port(number, tag, protocol);
        }

        public Port CreatePort(string tag, PortProtocol protocol)
        {
            return new Port(portNumberSource.GetNextNumber(), tag, protocol);
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
