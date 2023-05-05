using k8s;
using Utils;

namespace KubernetesWorkflow
{
    public class CommandRunner
    {
        private readonly K8sClient client;
        private readonly string k8sNamespace;
        private readonly RunningPod pod;
        private readonly string containerName;
        private readonly string command;
        private readonly string[] arguments;
        private readonly List<string> lines = new List<string>();

        public CommandRunner(K8sClient client, string k8sNamespace, RunningPod pod, string containerName, string command, string[] arguments)
        {
            this.client = client;
            this.k8sNamespace = k8sNamespace;
            this.pod = pod;
            this.containerName = containerName;
            this.command = command;
            this.arguments = arguments;
        }

        public void Run()
        {
            var input = new[] { command }.Concat(arguments).ToArray();

            Time.Wait(client.Run(c => c.NamespacedPodExecAsync(
                pod.Name, k8sNamespace, containerName, input, false, Callback, new CancellationToken())));
        }

        public string GetStdOut()
        {
            return string.Join(Environment.NewLine, lines);
        }

        private Task Callback(Stream stdIn, Stream stdOut, Stream stdErr)
        {
            using var streamReader = new StreamReader(stdOut);
            var line = streamReader.ReadLine();
            while (line != null)
            {
                lines.Add(line);
                line = streamReader.ReadLine();
            }

            return Task.CompletedTask;
        }
    }
}
