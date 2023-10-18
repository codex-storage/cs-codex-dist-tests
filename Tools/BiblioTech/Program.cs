using ArgsUniform;
using Core;
using Discord;
using Discord.WebSocket;
using Logging;

namespace BiblioTech
{
    public class Program
    {
        private DiscordSocketClient client = null!;

        public static Configuration Config { get; private set; } = null!;
        public static EndpointsFilesMonitor DeploymentFilesMonitor { get; } = new EndpointsFilesMonitor();

        public static Task Main(string[] args)
        {
            var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
            Config = uniformArgs.Parse();

            return new Program().MainAsync();
        }

        public async Task MainAsync()
        {
            Console.WriteLine("Starting Codex Discord Bot...");
            client = new DiscordSocketClient();
            client.Log += Log;

            ProjectPlugin.Load<CodexPlugin.CodexPlugin>();
            var entryPoint = new EntryPoint(new ConsoleLog(), new KubernetesWorkflow.Configuration(
                kubeConfigFile: null, // todo: readonly file
                operationTimeout: TimeSpan.FromMinutes(5),
                retryDelay: TimeSpan.FromSeconds(10),
                kubernetesNamespace: "not-applicable"), "datafiles");

            var fileMonitor = new EndpointsFilesMonitor();
            var monitor = new EndpointsMonitor(fileMonitor, entryPoint);

            var statusCommand = new StatusCommand(client, monitor);
            //var helloWorld = new HelloWorldCommand(client); Example for how to do arguments.

            await client.LoginAsync(TokenType.Bot, Config.ApplicationToken);
            await client.StartAsync();
            Console.WriteLine("Running...");
            await Task.Delay(-1);
        }

        private static void PrintHelp()
        {
            Console.WriteLine("BiblioTech - Codex Discord Bot");
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
