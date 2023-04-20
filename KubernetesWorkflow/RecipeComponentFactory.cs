using System.Globalization;
using Utils;

namespace KubernetesWorkflow
{
    public class RecipeComponentFactory
    {
        private NumberSource portNumberSource = new NumberSource(8080);

        public Port CreatePort(string tag)
        {
            return new Port(portNumberSource.GetNextNumber(), tag);
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
