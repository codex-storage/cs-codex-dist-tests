using ArgsUniform;
using AutoClient;
using AutoClient.Modes;
using AutoClient.Modes.FolderStore;
using CodexOpenApi;
using Utils;

public class Program
{
    private readonly App app;
    private readonly List<IMode> modes = new List<IMode>();

    public Program(Configuration config)
    {
        app = new App(config);
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

        var p = new Program(config);
        await p.Run();
    }

    public async Task Run()
    {
        var codexInstances = await CreateCodexInstances();

        var i = 0;
        foreach (var cdx in codexInstances)
        {
            var mode = CreateMode();
            modes.Add(mode);

            mode.Start(cdx, i);
            i++;
        }

        app.Cts.Token.WaitHandle.WaitOne();

        foreach (var mode in modes) mode.Stop();
        modes.Clear();

        app.Log.Log("Done");
    }

    private IMode CreateMode()
    {
        if (!string.IsNullOrEmpty(app.Config.FolderToStore))
        {
            return CreateFolderStoreMode();
        }

        return new PurchasingMode(app);
    }

    private IMode CreateFolderStoreMode()
    {
        if (app.Config.ContractDurationMinutes - 1 < 5) throw new Exception("Contract duration config option not long enough!");

        return new FolderStoreMode(app, app.Config.FolderToStore, new PurchaseInfo
        {
            PurchaseDurationTotal = TimeSpan.FromMinutes(app.Config.ContractDurationMinutes),
            PurchaseDurationSafe = TimeSpan.FromMinutes(app.Config.ContractDurationMinutes - 1),
        });
    }

    private async Task<CodexInstance[]> CreateCodexInstances()
    {
        var endpointStrs = app.Config.CodexEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<CodexInstance>();

        foreach (var e in endpointStrs)
        {
            result.Add(await CreateCodexInstance(e));
        }

        return result.ToArray();
    }

    private async Task<CodexInstance> CreateCodexInstance(string endpoint)
    {
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

        var address = new Address(
            host: host,
            port: port
        );

        var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(60.0);
        var codex = new CodexApi(client);
        codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

        app.Log.Log($"Checking Codex at {address}...");
        await CheckCodex(codex);
        app.Log.Log("OK");

        return new CodexInstance(
            app,
            codex,
            client,
            address
        );
    }

    private async Task CheckCodex(CodexApi codex)
    {
        try
        {
            var info = await codex.GetDebugInfoAsync();
            if (string.IsNullOrEmpty(info.Id)) throw new Exception("Failed to fetch Codex node id");
        }
        catch (Exception ex)
        {
            app.Log.Error($"Codex not OK: {ex}");
            throw;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Codex storage contracts for it.");
    }
}
