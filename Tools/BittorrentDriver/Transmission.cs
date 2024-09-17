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

        public string CreateNew(int size, string trackerUrl)
        {
            var file = CreateFile(size);

            var outFile = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString());

            var base64 = CreateTorrentFile(file, outFile, trackerUrl);

            if (File.Exists(outFile)) File.Delete(outFile);

            return base64;
        }

        public string Download(string torrentBase64)
        {
            var torrentFile = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".torrent");
            File.WriteAllBytes(torrentFile, Convert.FromBase64String(torrentBase64));

            var info = new ProcessStartInfo
            {
                FileName = "transmission-cli",
                Arguments = torrentFile
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
            var process = Process.Start(info);
            if (process == null) throw new Exception("Failed to start");
            process.WaitForExit(TimeSpan.FromMinutes(3));
            return process;
        }

        private TrackedFile CreateFile(int size)
        {
            try
            {
                var fileManager = new FileManager(log, dataDir);
                return fileManager.GenerateFile(size.Bytes());
            }
            catch (Exception ex)
            {
                log.Error("Failed to create file: " + ex);
                throw;
            }
        }
    }
}
