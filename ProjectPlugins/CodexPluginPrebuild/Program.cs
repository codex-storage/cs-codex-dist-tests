using System.Security.Cryptography;
using System.Text;

public static class Program
{
    private const string Search = "<INSERT-OPENAPI-YAML-HASH>";
    private const string CodexPluginFolderName = "CodexPlugin";
    private const string ProjectPluginsFolderName = "ProjectPlugins";

    public static void Main(string[] args)
    {
        Console.WriteLine("Injecting hash of 'openapi.yaml'...");

        var root = FindCodexPluginFolder();
        Console.WriteLine("Located CodexPlugin: " + root);
        var openApiFile = Path.Combine(root, "openapi.yaml");
        var clientFile = Path.Combine(root, "obj", "openapiClient.cs");
        var targetFile = Path.Combine(root, "ApiChecker.cs");

        // Force client rebuild by deleting previous artifact.
        File.Delete(clientFile);

        var hash = CreateHash(openApiFile);
        // This hash is used to verify that the Codex docker image being used is compatible
        // with the openapi.yaml being used by the Codex plugin.
        // If the openapi.yaml files don't match, an exception is thrown.

        SearchAndInject(hash, targetFile);

        // This program runs as the pre-build trigger for "CodexPlugin".
        // You might be wondering why this work isn't done by a shell script.
        // This is because this project is being run on many different platforms.
        // (Mac, Unix, Win, but also desktop/cloud containers.)
        // In order to not go insane trying to make a shell script that works in all possible cases,
        // instead we use the one tool that's definitely installed in all platforms and locations
        // when you're trying to run this plugin.

        Console.WriteLine("Done!");
    }

    private static string FindCodexPluginFolder()
    {
        var current = Directory.GetCurrentDirectory();

        while (true)
        {
            var localFolders = Directory.GetDirectories(current);
            var projectPluginsFolders = localFolders.Where(l => l.EndsWith(ProjectPluginsFolderName)).ToArray();
            if (projectPluginsFolders.Length == 1)
            {
                return Path.Combine(projectPluginsFolders.Single(), CodexPluginFolderName);
            }
            var codexPluginFolders = localFolders.Where(l => l.EndsWith(CodexPluginFolderName)).ToArray();
            if (codexPluginFolders.Length == 1)
            {
                return codexPluginFolders.Single();
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                var msg = $"Unable to locate '{CodexPluginFolderName}' folder. Travelled up from: '{Directory.GetCurrentDirectory()}'";
                Console.WriteLine(msg);
                throw new Exception(msg);
            }

            current = parent.FullName;
        }
    }

    private static string CreateHash(string openApiFile)
    {
        var file = File.ReadAllText(openApiFile);
        var fileBytes = Encoding.ASCII.GetBytes(file
            .Replace(Environment.NewLine, ""));

        var sha = SHA256.Create();
        var hash = sha.ComputeHash(fileBytes);
        return BitConverter.ToString(hash);
    }

    private static void SearchAndInject(string hash, string targetFile)
    {
        var lines = File.ReadAllLines(targetFile);
        Inject(lines, hash);
        File.WriteAllLines(targetFile, lines);
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