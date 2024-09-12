using ArgsUniform;
using AutoClient;
using CodexOpenApi;
using Utils;

public class Program
{
    private readonly App app;

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
        var codexUsers = await CreateUsers();

        var i = 0;
        foreach (var user in codexUsers)
        {
            user.Start(i);
            i++;
        }

        app.Cts.Token.WaitHandle.WaitOne();

        foreach (var user in codexUsers) user.Stop();

        app.Log.Log("Done");
    }

    private async Task<CodexUser[]> CreateUsers()
    {
        var endpointStrs = app.Config.CodexEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
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

        var client = new HttpClient();
        var codex = new CodexApi(client);
        codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

        app.Log.Log($"Checking Codex at {address}...");
        await CheckCodex(codex);
        app.Log.Log("OK");

        return new CodexUser(
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