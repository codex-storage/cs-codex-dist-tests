using Logging;

public class Program
{
    public static void Main(string[] args)
    {
        args = ["d:\\CodexTestLogs\\BlockExchange\\experiment2-fetchbatched"];
        var p = new Program(args[0]);
        p.Run();
    }

    private static readonly ILog log = new ConsoleLog();
    private string path;

    private readonly Dictionary<string, List<string>> combine = new Dictionary<string, List<string>>();

    public Program(string path)
    {
        this.path = path;
    }

    private void Run()
    {
        Log("Starting in " + path);

        var files = Directory.GetFiles(path)
            .Where(f => f.ToLowerInvariant().EndsWith(".csv")).ToArray();

        foreach (var file in files)
        {
            AddToMap(file);
        }

        var i = 0;
        foreach (var pair in combine)
        {
            var list = pair.Value;
            list.Insert(0, pair.Key);

            File.WriteAllLines(Path.Combine(path, "combine_" + i + ".csv"), list.ToArray());
            i++;
        }

        Log("done");
    }

    private void AddToMap(string file)
    {
        var lines = File.ReadAllLines(file);
        if (lines.Length > 1)
        {
            var header = lines[0];
            var list = GetList(header);
            list.AddRange(lines.Skip(1));
        }
    }

    private List<string> GetList(string header)
    {
        if (!combine.ContainsKey(header))
        {
            combine.Add(header, new List<string>());
        }
        return combine[header];
    }

    private void Log(string msg)
    {
        log.Log(msg);
    }
}