using ContinuousTests;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Codex Continous-Test-Runner.");
        Console.WriteLine("Running...");

        var cts = new CancellationTokenSource();
        var runner = new ContinuousTestRunner(args, cts.Token);

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Stopping...");
            e.Cancel = true;

            cts.Cancel();
        };
        
        runner.Run();
        Console.WriteLine("Done.");
    }
}
