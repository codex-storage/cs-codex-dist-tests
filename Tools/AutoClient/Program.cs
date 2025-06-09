using ArgsUniform;
using AutoClient;
using AutoClient.Modes;
using CodexClient;
using GethPlugin;
using Utils;
using WebUtils;
using Logging;

public class Program
{
    private readonly App app;

    public Program(Configuration config)
    {
        app = new App(config);
    }

    public static void Main(string[] args)
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
        p.Run();
    }

    public void Run()
    {
        if (app.Config.ContractDurationMinutes - 1 < 5) throw new Exception("Contract duration config option not long enough!");
        Log("Setting up Codex instances...");
        var codexNodes = CreateCodexWrappers();
        var loadBalancer = new LoadBalancer(app, codexNodes);
        Log("Setting up load-balancer...");
        loadBalancer.Start();

        var folderStore = new FolderStoreMode(app, loadBalancer);
        Log("Starting folder store mode...");
        folderStore.Start();

        app.Cts.Token.WaitHandle.WaitOne();

        folderStore.Stop();
        loadBalancer.Stop();

        Log("Done");
    }

    private CodexWrapper[] CreateCodexWrappers()
    {
        var endpointStrs = app.Config.CodexEndpoints.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<CodexWrapper>();

        var i = 1;
        foreach (var e in endpointStrs)
        {
            result.Add(CreateCodexWrapper(e, i));
            i++;
        }

        return result.ToArray();
    }

    private readonly string LogLevel = "TRACE;info:discv5,providers,routingtable,manager,cache;warn:libp2p,multistream,switch,transport,tcptransport,semaphore,asyncstreamwrapper,lpstream,mplex,mplexchannel,noise,bufferstream,mplexcoder,secure,chronosstream,connection,websock,ws-session,muxedupgrade,upgrade,identify,contracts,clock,serde,json,serialization,JSONRPC-WS-CLIENT,JSONRPC-HTTP-CLIENT,repostore";

    private CodexWrapper CreateCodexWrapper(string endpoint, int number)
    {
        var splitIndex = endpoint.LastIndexOf(':');
        var host = endpoint.Substring(0, splitIndex);
        var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

        var address = new Address(
            logName: $"cdx@{host}:{port}",
            host: host,
            port: port
        );

        var numberStr = number.ToString().PadLeft(3, '0');
        var log = new LogPrefixer(app.Log, $"[{numberStr}] ");
        var httpFactory = new HttpFactory(log, new AutoClientWebTimeSet());
        var codexNodeFactory = new CodexNodeFactory(log: log, httpFactory: httpFactory, dataDir: app.Config.DataPath);
        var instance = CodexInstance.CreateFromApiEndpoint($"[AC-{numberStr}]", address, EthAccountGenerator.GenerateNew());
        var node = codexNodeFactory.CreateCodexNode(instance);

        node.SetLogLevel(LogLevel);
        Log($"Set up codex endpoint: {numberStr}");
        return new CodexWrapper(app, node);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Generates fake data and creates Codex storage contracts for it.");
    }

    private void Log(string msg)
    {
        app.Log.Log(msg);
    }
}
