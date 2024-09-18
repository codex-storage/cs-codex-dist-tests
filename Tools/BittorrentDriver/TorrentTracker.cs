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
                Arguments = 
                    $"--port {port} " +
                    $"--http " +
                    $"--stats " +
                    $"--interval=3000 " + // 3 seconds
                    $"&",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            process = Process.Start(info);
            if (process == null) return "Failed to start";

            process.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine("STDOUT: " + args.Data);
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                Console.WriteLine("STDERR: " + args.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

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
