using LibGit2Sharp;
using CodexTests.BasicTests;
using CodexPlugin;
using System.Diagnostics;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var codexPath = @"D:\Projects\nim-codex";

        using var repo = new Repository(codexPath);
        var masterBranch = repo.Branches.Single(b => b.FriendlyName == "master");
        var take = masterBranch.Commits.Take(10).ToArray();
        foreach (var c in take)
        {
            RunWith(c);
        }

        Console.ReadLine();
    }

    private static void RunWith(Commit c)
    {
        var shortSha = c.Sha.Substring(0, 7);
        var image = $"codexstorage/nim-codex:sha-{shortSha}-dist-tests";
        CodexContainerRecipe.DockerImageOverride = image;

        if (!DoesExist(image))
        {
            return;
        }

        try
        {
            Console.WriteLine("Running test for: " + c.MessageShort);

            var test = new ExampleTests();
            test.GlobalSetup();
            test.SetUpDistTest();

            try
            {
                test.CodexLogExample();
            }
            catch (Exception ex) 
            {
                Console.WriteLine("test ex: " + ex);
            }

            test.TearDownDistTest();
            test.GlobalTearDown();

            Console.WriteLine("Test passed");
        }
        catch (Exception a)
        {
            Console.WriteLine("ex: " + a);
        }
    }

    private static bool DoesExist(string image)
    {
        var info = new ProcessStartInfo("docker", "image pull " + image);
        info.RedirectStandardOutput = false;
        var process = Process.Start(info);
        process.WaitForExit();

        return process.ExitCode == 0;
    }
}

