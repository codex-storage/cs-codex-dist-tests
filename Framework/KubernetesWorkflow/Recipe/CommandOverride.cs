namespace KubernetesWorkflow.Recipe
{
    public class CommandOverride
    {
        public CommandOverride(params string[] command)
        {
            Command = command;
        }

        public string[] Command { get; }
    }
}
