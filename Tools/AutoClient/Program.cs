using ArgsUniform;
using AutoClient;
using CodexOpenApi;
using Core;
using Logging;
using Nethereum.Model;
using Utils;

public class Program
{
    private readonly CancellationTokenSource cts;
    private readonly Configuration config;
    private readonly LogSplitter log;
    private readonly IFileGenerator generator;

    public Program(CancellationTokenSource cts, Configuration config, LogSplitter log, IFileGenerator generator)
    {
        this.cts = cts;
        this.config = config;
        this.log = log;
        this.generator = generator;
    }

    public static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
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

        var generator = CreateGenerator(config, log);

        var p = new Program(cts, config, log, generator);
        await p.Run(args);
        cts.Token.WaitHandle.WaitOne();
        log.Log("Done.");
    }

    public async Task Run(string[] args)
    {
        var codexUsers = CreateUsers();


        
    }

    private async Task<CodexUser[]> CreateUsers()
    {
        var endpointStrs = config.CodexEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<CodexUser>();

        foreach (var e in endpointStrs)
        {
            result.Add(await CreateUser(e));
        }

        return result.ToArray();
    }

    private async Task<CodexUser> CreateUser(string endpoint)
    {
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

        var address = new Address(
            host: host,
            port: port
        );

        log.Log($"Start. Address: {address}");


        var client = new HttpClient();
        var codex = new CodexApi(client);
        codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

        await CheckCodex(codex);

        return new CodexUser();
    }

    private async Task CheckCodex(CodexApi codex)
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

    private static IFileGenerator CreateGenerator(Configuration config, LogSplitter log)
    {
        if (config.FileSizeMb > 0)
        {
            return new RandomFileGenerator(config, log);
        }
        return new ImageGenerator(log);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Codex storage contracts for it.");
    }
}