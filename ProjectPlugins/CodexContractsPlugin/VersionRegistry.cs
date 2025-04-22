using System.Diagnostics;
using Logging;

namespace CodexContractsPlugin
{
    public interface ICodexDockerImageProvider
    {
        string GetCodexDockerImage();
    }

    public class VersionRegistry
    {
        private ICodexDockerImageProvider provider = new ExceptionProvider();
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private static readonly object cacheLock = new object();
        private readonly ILog log;

        public VersionRegistry(ILog log)
        {
            this.log = log;
        }

        public void SetProvider(ICodexDockerImageProvider provider)
        {
            this.provider = provider;
        }

        public string GetContractsDockerImage()
        {
            try
            {
                var codexImage = provider.GetCodexDockerImage();
                return GetContractsDockerImage(codexImage);
            }
            catch (Exception exc)
            {
                throw new Exception("Failed to get contracts docker image.", exc);
            }
        }

        private string GetContractsDockerImage(string codexImage)
        {
            lock (cacheLock)
            {
                if (cache.TryGetValue(codexImage, out string? value))
                {
                    return value;
                }
                var result = GetContractsImage(codexImage);
                cache.Add(codexImage, result);
                return result;
            }
        }

        private string GetContractsImage(string codexImage)
        {
            var inspectResult = InspectCodexImage(codexImage);
            var image = ParseCodexContractsImageName(inspectResult);
            log.Log($"From codex docker image '{codexImage}', determined codex-contracts docker image: '{image}'");
            return image;
        }

        private string InspectCodexImage(string img)
        {
            Execute("docker", $"pull {img}");
            return Execute("docker", $"inspect {img}");
        }

        private string ParseCodexContractsImageName(string inspectResult)
        {
            // It is a nice json structure. But we only need this one line.
            // "storage.codex.nim-codex.blockchain-image": "codexstorage/codex-contracts-eth:sha-0bf1385-dist-tests"
            var lines = inspectResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var line = lines.Single(l => l.Contains("storage.codex.nim-codex.blockchain-image"));
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Last().Replace("\"", "").Trim();
        }

        private string Execute(string cmd, string args)
        {
            var startInfo = new ProcessStartInfo(
                fileName: cmd,
                arguments: args
            );
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start: " + cmd + args);
            }
            KillAfterTimeout(process);

            process.WaitForExit();
            return process.StandardOutput.ReadToEnd();
        }

        private void KillAfterTimeout(Process process)
        {
            // There's a known issue that some docker commands on some platforms
            // will fail to stop on their own. This has been known since 2019 and it's not fixed.
            // So we will issue a kill to the process ourselves if it exceeds a timeout.

            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(30.0));

                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            });
        }
    }

    internal class ExceptionProvider : ICodexDockerImageProvider
    {
        public string GetCodexDockerImage()
        {
            throw new InvalidOperationException("CodexContractsPlugin has not yet received a CodexDockerImageProvider " +
                "and so cannot select a compatible contracts docker image.");
        }
    }
}
