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

        if (config.NumConcurrentPurchases < 1)
        {
            throw new Exception("Number of concurrent purchases must be > 0");
        }

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

        var purchasers = new List<Purchaser>();
        for (var i = 0; i < config.NumConcurrentPurchases; i++)
        {
            purchasers.Add(
                new Purchaser(new LogPrefixer(log, $"({i}) "), client, address, codex, cancellationToken, config, imgGenerator)
            );
        }

        var delayPerPurchaser = TimeSpan.FromMinutes(config.ContractDurationMinutes) / config.NumConcurrentPurchases;
        foreach (var purchaser in purchasers) 
        {
            purchaser.Start();
            await Task.Delay(delayPerPurchaser);
        }

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
}