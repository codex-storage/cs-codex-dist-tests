using ArgsUniform;
using AutoClient;
using CodexOpenApi;
using Core;
using Logging;

public static class Program
{
    public static async Task Main(string[] args)
    {

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        Console.CancelKeyPress += (sender, args) => cts.Cancel();

        var uniformArgs = new ArgsUniform<Configuration>(PrintHelp, args);
        var config = uniformArgs.Parse(true);

        var log = new LogSplitter(
            new FileLog(Path.Combine(config.LogPath, "autoclient")),
            new ConsoleLog()
        );

        var address = new Utils.Address(
            host: config.CodexHost,
            port: config.CodexPort
        );

        log.Log($"Start. Address: {address}");

        var imgGenerator = new ImageGenerator();

        var client = new HttpClient();
        var codex = new CodexApi(client);
        codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

        await CheckCodex(codex, log);

        var runner = new Runner(log, client, address, codex, cancellationToken, config, imgGenerator);
        await runner.Run();

        log.Log("Done.");
    }

    private static async Task CheckCodex(CodexApi codex, ILog log)
    {
        log.Log("Checking Codex...");
        try
        {
            var info = await codex.GetDebugInfoAsync();
            if (string.IsNullOrEmpty(info.Id)) throw new Exception("Failed to fetch Codex node id");
        }
        catch (Exception ex)
        {
            log.Log($"Codex not OK: {ex}");
            throw;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Codex storage contracts for it.");
    }

    private static IPluginTools CreateTools(ILog log, Configuration config)
    {
        var configuration = new KubernetesWorkflow.Configuration(
            null,
            operationTimeout: TimeSpan.FromMinutes(10),
            retryDelay: TimeSpan.FromSeconds(10),
            kubernetesNamespace: "notUsed!#");

        var result = new EntryPoint(log, configuration, config.DataPath, new DefaultTimeSet());
        return result.Tools;
    }
}