using Logging;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BittorrentDriver.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TorrentController : ControllerBase
    {
        private readonly ILog log = new ConsoleLog();
        private readonly TorrentTracker tracker = new TorrentTracker();
        private readonly Transmission transmission;

        public TorrentController()
        {
            transmission = new Transmission(log);
        }

        [HttpPut("tracker")]
        public string StartTracker([FromBody] int port)
        {
            return Try(() =>
            {
                Log("Starting tracker...");
                return tracker.Start(port);
            });
        }

        [HttpPut("daemon")]
        public string StartDaemon([FromBody] int peerPort)
        {
            return Try(() =>
            {
                Log("Starting daemon...");
                return transmission.StartDaemon(peerPort);
            });
        }

        [HttpPost("create")]
        public string CreateTorrent([FromBody] CreateTorrentInput input)
        {
            return Try(() =>
            {
                Log("Creating torrent file...");
                return transmission.CreateNew(input.Size, input.TrackerUrl);
            });
        }

        [HttpPost("download")]
        public string DownloadTorrent([FromBody] DownloadTorrentInput input)
        {
            return Try(() =>
            {
                Log("Downloading torrent...");
                return transmission.Download(input.TorrentBase64);
            });
        }

        private string Try(Func<string> value)
        {
            try
            {
                return value();
            }
            catch (Exception exc)
            {
                log.Error(exc.ToString());
                return exc.ToString();
            }
        }

        private void Log(string v)
        {
            log.Log(v);
        }
    }

    public class CreateTorrentInput
    {
        public int Size { get; set; }
        public string TrackerUrl { get; set; } = string.Empty;
    }

    public class DownloadTorrentInput
    {
        public string TorrentBase64 { get; set; } = string.Empty;
    }
}
