using Core;
using KubernetesWorkflow.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace BittorrentPlugin
{
    public interface IBittorrentNode
    {
        string StartAsTracker();
        string CreateTorrent(ByteSize size, IBittorrentNode tracker);
        string StartDaemon();
        string DownloadTorrent(string torrent);
    }

    public class BittorrentNode : IBittorrentNode
    {
        private readonly IPluginTools tools;
        private readonly RunningContainer container;

        public BittorrentNode(IPluginTools tools, RunningContainer container)
        {
            this.tools = tools;
            this.container = container;
        }

        public string CreateTorrent(ByteSize size, IBittorrentNode tracker)
        {
            var trackerUrl = ((BittorrentNode)tracker).TrackerAddress;
            var endpoint = GetEndpoint();

            var torrent = endpoint.HttpPostJson("create", new CreateTorrentRequest
            {
                Size = Convert.ToInt32(size.SizeInBytes),
                TrackerUrl = trackerUrl.ToString()
            });

            return torrent;
        }

        public string StartDaemon()
        {
            var endpoint = GetEndpoint();
            return endpoint.HttpPutString("daemon", BittorrentContainerRecipe.PeerPort.ToString());
        }

        public string DownloadTorrent(string torrent)
        {
            var endpoint = GetEndpoint();

            return endpoint.HttpPostString<string>("download", torrent);
        }

        public string StartAsTracker()
        {
            TrackerAddress = container.GetAddress(tools.GetLog(), BittorrentContainerRecipe.TrackerPortTag);
            var endpoint = GetEndpoint();

            return endpoint.HttpPutString("tracker", BittorrentContainerRecipe.TrackerPort.ToString());
        }

        public Address TrackerAddress { get; private set; } = new Address("", 0);

        public class CreateTorrentRequest
        {
            public int Size { get; set; }
            public string TrackerUrl { get; set; } = string.Empty;
        }

        private IEndpoint GetEndpoint()
        {
            var address = container.GetAddress(tools.GetLog(), BittorrentContainerRecipe.ApiPortTag);
            var http = tools.CreateHttp(address.ToString(), c => { });
            return http.CreateEndpoint(address, "/torrent/", container.Name);
        }
    }
}
