using System.Security.Cryptography;

public static class Program
{
    private const string OpenApiFile = "../CodexPlugin/openapi.yaml";
    private const string Search = "<CODEX_OPENAPI_HASH_HERE>";
    private const string TargetFile = "CodexPlugin.cs";

    private static string CreateHash()
    {
        var fileBytes = File.ReadAllBytes(OpenApiFile);
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(fileBytes);
        return BitConverter.ToString(hash);
    }

    private static void SearchAndReplace(string hash)
    {
        var lines = File.ReadAllLines(TargetFile);
        lines = lines.Select(l => l.Replace(Search, hash)).ToArray();
        File.WriteAllLines(TargetFile, lines);
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Injecting hash of 'openapi.yaml'...");
        // This hash is used to verify that the Codex docker image being used is compatible
        // with the openapi.yaml being used by the Codex plugin.
        // If the openapi.yaml files don't match, an exception is thrown.

        var hash = CreateHash();
        SearchAndReplace(hash);

        Console.WriteLine("Done!");
    }
}