﻿using Logging;
using OverwatchTranscript;
using TranscriptAnalysis;

public static class Program
{
    private static ILog log;

    public static void Main(string[] args)
    {
        log = new ConsoleLog();
        args = new[] { "D:\\Projects\\cs-codex-dist-tests\\Tests\\CodexTests\\bin\\Debug\\net7.0\\CodexTestLogs\\2024-08\\06\\08-24-45Z_ThreeClientTest\\SwarmTest_SwarmTest.owts" };

        Log("Transcript Analysis");
        if (!args.Any())
        {
            Log("Please pass a .owts file");
            Console.ReadLine();
            return;
        }

        if (!File.Exists(args[0]))
        {
            Log("File doesn't exist: " + args[0]);
            Console.ReadLine();
            return;
        }

        var reader = OpenReader(args[0]);
        AppDomain.CurrentDomain.ProcessExit += (e, s) =>
        {
            CloseReader(reader);
        };

        var duplicatesReceived = new DuplicateBlocksReceived(log, reader);

        var processor = new Processor(log, reader);
        processor.RunAll();

        duplicatesReceived.Finish();

        CloseReader(reader);
        Log("Done.");
        Console.ReadLine();
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
