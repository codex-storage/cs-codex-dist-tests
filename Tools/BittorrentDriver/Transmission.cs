using FileUtils;
using Logging;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using Utils;

namespace BittorrentDriver
{
    public class Transmission
    {
        private readonly string dataDir;
        private readonly ILog log;

        public Transmission(ILog log)
        {
            dataDir = Path.Combine(Directory.GetCurrentDirectory(), "files");
            Directory.CreateDirectory(dataDir);
            this.log = log;
        }

        public CreateTorrentResult CreateNew(int size, string trackerUrl)
        {
            var file = CreateFile(size);
            var outFile = Path.Combine(dataDir, Guid.NewGuid().ToString());
            var base64 = CreateTorrentFile(file, outFile, trackerUrl);
            return new CreateTorrentResult
            {
                LocalFilePath = outFile,
                TorrentBase64 = base64
            };
        }

        public string StartDaemon(int peerPort)
        {
            var info = new ProcessStartInfo
            {
                FileName = "transmission-daemon",
                Arguments = $"--peerport={peerPort} " +
                $"--download-dir={dataDir} " +
                $"--watch-dir={dataDir} " +
                $"--no-global-seedratio " +
                $"--bind-address-ipv4=0.0.0.0 " +
                $"--dht"
            };
            RunToComplete(info);

            return "OK";
        }
        
        public string AddTracker(string trackerUrl, string localFile)
        {
            var info = new ProcessStartInfo
            {
                FileName = "transmission-edit",
                Arguments = $"--add={trackerUrl} {localFile}"
            };
            RunToComplete(info);

            return "OK";
        }

        public string PutLocalFile(string torrentBase64)
        {
            var torrentFile = Path.Combine(dataDir, Guid.NewGuid().ToString() + ".torrent");
            File.WriteAllBytes(torrentFile, Convert.FromBase64String(torrentBase64));
            return torrentFile;
        }

        public string Download(string localFile)
        {
            var peerPort = Environment.GetEnvironmentVariable("PEERPORT");

            var info = new ProcessStartInfo
            {
                FileName = "transmission-cli",
                Arguments = 
                    $"--port={peerPort} " +
                    $"--download-dir={dataDir} " +
                    $"{localFile}"
            };
            RunToComplete(info);

            return "OK";
        }

        private string CreateTorrentFile(TrackedFile file, string outFile, string trackerUrl)
        {
            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = "transmission-create",
                    Arguments = $"-o {outFile} -t {trackerUrl} {file.Filename}",
                };

                var process = RunToComplete(info);

                log.Log(nameof(CreateTorrentFile) + " exited with: " + process.ExitCode);

                if (!File.Exists(outFile)) throw new Exception("Outfile not created.");

                return Convert.ToBase64String(File.ReadAllBytes(outFile));
            }
            catch (Exception ex)
            {
                log.Error("Failed to create torrent file: " + ex);
                throw;
            }
        }

        private Process RunToComplete(ProcessStartInfo info)
        {
            log.Log($"Running: {info.FileName} ({info.Arguments})");
            var process = Process.Start(info);
            if (process == null) throw new Exception("Failed to start");
            process.WaitForExit(TimeSpan.FromMinutes(3));
            return process;
        }

        private TrackedFile CreateFile(int size)
        {
            try
            {
                var fileManager = new FileManager(log, dataDir, numberSubfolders: false);
                var file = fileManager.GenerateFile(size.Bytes());
                log.Log("Generated file: " + file.Filename);
                return file;
            }
            catch (Exception ex)
            {
                log.Error("Failed to create file: " + ex);
                throw;
            }
        }
    }

    public class CreateTorrentResult
    {
        public string LocalFilePath { get; set; } = string.Empty;
        public string TorrentBase64 { get; set; } = string.Empty;
    } 
}
