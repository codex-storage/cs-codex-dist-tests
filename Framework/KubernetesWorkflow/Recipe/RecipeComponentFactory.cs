using System.Globalization;
using Utils;

namespace KubernetesWorkflow.Recipe
{
    public class RecipeComponentFactory
    {
        private NumberSource internalNumberSource = new NumberSource(8080);
        private NumberSource externalNumberSource = new NumberSource(30000);

        public Port CreatePort(int number, string tag, PortProtocol protocol)
        {
            return new Port(number, tag, protocol);
        }

        public Port CreateInternalPort(string tag, PortProtocol protocol)
        {
            return new Port(internalNumberSource.GetNextNumber(), tag, protocol);
        }

        public Port CreateExternalPort(string tag, PortProtocol protocol)
        {
            return new Port(externalNumberSource.GetNextNumber(), tag, protocol);
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
