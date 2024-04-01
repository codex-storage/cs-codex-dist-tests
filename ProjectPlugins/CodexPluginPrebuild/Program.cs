using System.Security.Cryptography;
using System.Text;

public static class Program
{
    private const string OpenApiFile = "../CodexPlugin/openapi.yaml";
    private const string ClientFile = "../CodexPlugin/obj/openapiClient.cs";
    private const string Search = "<INSERT-OPENAPI-YAML-HASH>";
    private const string TargetFile = "ApiChecker.cs";

    public static void Main(string[] args)
    {
        Console.WriteLine("Injecting hash of 'openapi.yaml'...");

        // Force client rebuild by deleting previous artifact.
        File.Delete(ClientFile);

        var hash = CreateHash();
        // This hash is used to verify that the Codex docker image being used is compatible
        // with the openapi.yaml being used by the Codex plugin.
        // If the openapi.yaml files don't match, an exception is thrown.

        SearchAndInject(hash);

        // This program runs as the pre-build trigger for "CodexPlugin".
        // You might be wondering why this work isn't done by a shell script.
        // This is because this project is being run on many different platforms.
        // (Mac, Unix, Win, but also desktop/cloud containers.)
        // In order to not go insane trying to make a shell script that works in all possible cases,
        // instead we use the one tool that's definitely installed in all platforms and locations
        // when you're trying to run this plugin.

        Console.WriteLine("Done!");
    }

    private static string CreateHash()
    {
        var file = File.ReadAllText(OpenApiFile);
        var fileBytes = Encoding.ASCII.GetBytes(file
            .Replace(Environment.NewLine, ""));

        var sha = SHA256.Create();
        var hash = sha.ComputeHash(fileBytes);
        return BitConverter.ToString(hash);
    }

    private static void SearchAndInject(string hash)
    {
        var lines = File.ReadAllLines(TargetFile);
        Inject(lines, hash);
        File.WriteAllLines(TargetFile, lines);
    }

    private static void Inject(string[] lines, string hash)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(Search))
            {
                lines[i + 1] = $"        private const string OpenApiYamlHash = \"{hash}\";";
                return;
            }
        }
    }
}