using System.Diagnostics;

namespace BittorrentDriver
{
    public class TorrentTracker
    {
        private Process? process;

        public string Start(int port)
        {
            if (process != null) throw new Exception("Already started");
            var info = new ProcessStartInfo
            {
                FileName = "bittorrent-tracker",
                Arguments = $"--port {port} &",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            process = Process.Start(info);
            if (process == null) return "Failed to start";

            Thread.Sleep(1000);

            if (process.HasExited)
            {
                return
                    $"STDOUT: {process.StandardOutput.ReadToEnd()} " +
                    $"STDERR: {process.StandardError.ReadToEnd()}";
            }
            return "OK";
        }

    }
}
