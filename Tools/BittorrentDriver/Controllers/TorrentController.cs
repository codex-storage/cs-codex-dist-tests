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

        [HttpPut("starttracker")]
        public string StartTracker([FromBody] int port)
        {
            return Try(() =>
            {
                Log("Starting tracker...");
                return tracker.Start(port);
            });
        }

        [HttpPost("addtracker")]
        public string AddTracker([FromBody] AddTrackerInput input)
        {
            return Try(() =>
            {
                Log("Adding tracker: " + input.TrackerUrl + " - " + input.LocalFile);
                return transmission.AddTracker(input.TrackerUrl, input.LocalFile);
            });
        }

        [HttpGet("stats")]
        public string GetTrackerStats()
        {
            return Try(() =>
            {
                return tracker.GetStats();
            });
        }

        [HttpPost("postfile")]
        public string PostFile([FromBody] PostFileInput input)
        {
            return Try(() =>
            {
                Log("Creating file..");
                var file = transmission.PutLocalFile(input.Base64Content);
                Log("File: " + file);
                return file;
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
        public CreateTorrentResult CreateTorrent([FromBody] CreateTorrentInput input)
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
                return transmission.Download(input.LocalFile);
            });
        }

        private T Try<T>(Func<T> value)
        {
            try
            {
                return value();
            }
            catch (Exception exc)
            {
                log.Error(exc.ToString());
                throw;
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

    public class AddTrackerInput
    {
        public string TrackerUrl { get; set; } = string.Empty;
        public string LocalFile { get; set; } = string.Empty;
    }

    public class DownloadTorrentInput
    {
        public string LocalFile { get; set; } = string.Empty;
    }

    public class PostFileInput
    {
        public string Base64Content { get; set; } = string.Empty;
    }
}
