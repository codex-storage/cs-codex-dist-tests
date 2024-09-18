using Core;
using KubernetesWorkflow.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace BittorrentPlugin
{
    public interface IBittorrentNode
    {
        string StartAsTracker();
        string AddTracker(IBittorrentNode tracker, string localFile);
        string PutFile(string base64);
        string GetTrackerStats();
        CreateTorrentResult CreateTorrent(ByteSize size, IBittorrentNode tracker);
        string StartDaemon();
        string DownloadTorrent(string LocalFile);
    }

    public class BittorrentNode : IBittorrentNode
    {
        private readonly IPluginTools tools;
        private readonly RunningContainer container;
        private readonly PodInfo podInfo;

        public BittorrentNode(IPluginTools tools, RunningContainer container)
        {
            this.tools = tools;
            this.container = container;
            podInfo = tools.CreateWorkflow().GetPodInfo(container);
        }

        public string StartAsTracker()
        {
            //TrackerAddress = container.GetInternalAddress(BittorrentContainerRecipe.TrackerPortTag);
            var endpoint = GetEndpoint();
            return endpoint.HttpPutString("starttracker", GetTrackerAddress().Port.ToString());
        }

        public string AddTracker(IBittorrentNode tracker, string localFile)
        {
            var endpoint = GetEndpoint();
            var trackerUrl = ((BittorrentNode)tracker).GetTrackerAddress();
            return endpoint.HttpPostJson("addtracker", new AddTrackerRequest
            {
                LocalFile = localFile,
                TrackerUrl = $"{trackerUrl}/announce"
            });
        }

        public string PutFile(string base64)
        {
            var endpoint = GetEndpoint();
            return endpoint.HttpPostJson("postfile", new PostFileRequest
            {
                Base64Content = base64
            });
        }

        public string StartDaemon()
        {
            var endpoint = GetEndpoint();
            var peerPortAddress = container.GetInternalAddress(BittorrentContainerRecipe.PeerPortTag);
            return endpoint.HttpPutString("daemon", peerPortAddress.Port.ToString());
        }

        public CreateTorrentResult CreateTorrent(ByteSize size, IBittorrentNode tracker)
        {
            var trackerUrl = ((BittorrentNode)tracker).GetTrackerAddress();
            var endpoint = GetEndpoint();

            var json = endpoint.HttpPostJson("create", new CreateTorrentRequest
            {
                Size = Convert.ToInt32(size.SizeInBytes),
                TrackerUrl = $"{trackerUrl}/announce"
            });

            return JsonConvert.DeserializeObject<CreateTorrentResult>(json)!;
        }

        public string DownloadTorrent(string localFile)
        {
            var endpoint = GetEndpoint();

            return endpoint.HttpPostJson("download", new DownloadTorrentRequest
            {
                LocalFile = localFile
            });
        }

        public string GetTrackerStats()
        {
            var endpoint = GetEndpoint();
            return endpoint.HttpGetString("stats");
        }

        //public Address TrackerAddress { get; private set; } = new Address("", 0);

        public Address GetTrackerAddress()
        {
            var address = container.GetInternalAddress(BittorrentContainerRecipe.TrackerPortTag);
            return new Address("http://" + podInfo.Ip, address.Port);
        }

        private IEndpoint GetEndpoint()
        {
            var address = container.GetAddress(BittorrentContainerRecipe.ApiPortTag);
            var http = tools.CreateHttp(address.ToString(), c => { });
            return http.CreateEndpoint(address, "/torrent/", container.Name);
        }
    }

    public class CreateTorrentRequest
    {
        public int Size { get; set; }
        public string TrackerUrl { get; set; } = string.Empty;
    }

    public class CreateTorrentResult
    {
        public string LocalFilePath { get; set; } = string.Empty;
        public string TorrentBase64 { get; set; } = string.Empty;
    }

    public class DownloadTorrentRequest
    {
        public string LocalFile { get; set; } = string.Empty;
    }

    public class AddTrackerRequest
    {
        public string TrackerUrl { get; set; } = string.Empty;
        public string LocalFile { get; set; } = string.Empty;
    }

    public class PostFileRequest
    {
        public string Base64Content { get; set; } = string.Empty;
    }
}
