using CodexPlugin.OverwatchSupport;
using Logging;
using OverwatchTranscript;
using TranscriptAnalysis;
using TranscriptAnalysis.Receivers;

public static class Program
{
    private static readonly ILog log = new ConsoleLog();

    public static void Main(string[] args)
    {
        Log("Transcript Analysis");

        var path1 = "d:\\Projects\\cs-codex-dist-tests\\Tests\\CodexTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2024-10\\11\\08-31-52Z_SwarmTests\\";
        var path2 = "d:\\Projects\\cs-codex-dist-tests\\Tests\\CodexTests\\bin\\Debug\\net8.0\\CodexTestLogs\\2024-10\\11\\09-28-29Z_SwarmTests\\";
        var files1 = new[]
        {
            (1, 3, "DetectBlockRetransmits[1,3]_swarm_retransmit.owts"),
            (1, 5, "DetectBlockRetransmits[1,5]_swarm_retransmit.owts"),
            (1, 10, "DetectBlockRetransmits[1,10]_swarm_retransmit.owts"),
            (1, 20, "DetectBlockRetransmits[1,20]_swarm_retransmit.owts"),
            (5, 3, "DetectBlockRetransmits[5,3]_swarm_retransmit.owts"),
            (5, 5, "DetectBlockRetransmits[5,5]_swarm_retransmit.owts"),
            (5, 10, "DetectBlockRetransmits[5,10]_swarm_retransmit.owts"),
            (5, 20, "DetectBlockRetransmits[5,20]_swarm_retransmit.owts"),
            (10, 5, "DetectBlockRetransmits[10,5]_swarm_retransmit.owts"),
            (10, 10, "DetectBlockRetransmits[10,10]_swarm_retransmit.owts")
        };
        var files2 = new[]
        {
            (10, 20, "DetectBlockRetransmits[10,20]_swarm_retransmit.owts"),
            (20, 3, "DetectBlockRetransmits[20,3]_swarm_retransmit.owts"),
            (20, 5, "DetectBlockRetransmits[20,5]_swarm_retransmit.owts"),
            (20, 10, "DetectBlockRetransmits[20,10]_swarm_retransmit.owts"),
            (20, 20, "DetectBlockRetransmits[20,20]_swarm_retransmit.owts")
        };

        var countLines = new List<int[]>();

        foreach (var file in files1)
        {
            var path = Path.Combine(path1, file.Item3);
            DuplicateBlocksReceived.Counts.Clear();
            Run(path);

            countLines.Add(new[] { file.Item1, file.Item2 }.Concat(DuplicateBlocksReceived.Counts).ToArray());
        }
        foreach (var file in files2)
        {
            var path = Path.Combine(path2, file.Item3);
            DuplicateBlocksReceived.Counts.Clear();
            Run(path);

            countLines.Add(new[] { file.Item1, file.Item2 }.Concat(DuplicateBlocksReceived.Counts).ToArray());
        }

        var numColumns = countLines.Max(l => l.Length);
        var header = new List<string>() { "filesize", "numNodes" };
        for (var i = 0; i < numColumns - 2; i++) header.Add("recv" + (i + 1) + "x");

        var lines = new List<string>() { string.Join(",", header.ToArray()) };
        foreach (var count in countLines)
        {
            var tokens = new List<int>();
            for (var i = 0; i < numColumns; i++)
            {
                if (i < count.Length) tokens.Add(count[i]);
                else tokens.Add(0);
            }
            lines.Add(string.Join(",", tokens.Select(t => t.ToString()).ToArray()));
        }

        File.WriteAllLines("C:\\Users\\Ben\\Desktop\\blockretransmit.csv", lines.ToArray());

        Log("Done.");
        Console.ReadLine();
    }

    private static void Run(string file)
    {
        var reader = OpenReader(file);
        var header = reader.GetHeader<OverwatchCodexHeader>("cdx_h");
        var receivers = new ReceiverSet(log, reader, header);
        receivers.InitAll();

        var processor = new Processor(log, reader);
        processor.RunAll();

        receivers.FinishAll();

        CloseReader(reader);

    }

    private static ITranscriptReader OpenReader(string filepath)
    {
        try
        {
            Log($"Opening: '{filepath}'");
            return Transcript.NewReader(filepath);
        }
        catch (Exception ex)
        {
            Log("Failed to open file for reading: " + ex);
            Console.ReadLine();
            Environment.Exit(1);
            return null;
        }
    }

    private static void CloseReader(ITranscriptReader reader)
    {
        try
        {
            Log("Closing...");
            reader.Close();
        }
        catch (Exception ex)
        {
            Log("Failed to close reader: " + ex);
        }
    }

    private static void Log(string msg)
    {
        log.Log(msg);
    }
}
