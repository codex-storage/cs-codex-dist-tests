using ContinuousTests;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Codex Continous-Test-Runner.");
        Console.WriteLine("Running...");
        var runner = new ContinuousTestRunner();
        runner.Run();
    }
}
