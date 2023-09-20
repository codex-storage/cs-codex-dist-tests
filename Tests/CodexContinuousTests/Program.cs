using ContinuousTests;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Codex Continous-Test-Runner.");
        Console.WriteLine("Running...");

        var runner = new ContinuousTestRunner(args, Cancellation.Cts.Token);

        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Stopping...");
            e.Cancel = true;

            Cancellation.Cts.Cancel();
        };
        
        runner.Run();
        Console.WriteLine("Done.");
    }

    public static class Cancellation
    {
        static Cancellation()
        {
            Cts = new CancellationTokenSource();
        }

        public static CancellationTokenSource Cts { get; } 
    }
}
