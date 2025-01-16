using CodexClient;

namespace CodexPlugin.OverwatchSupport.LineConverters
{
    public class BootstrapLineConverter : ILineConverter
    {
        private const string peerIdTag = "peerId: ";

        public string Interest => "Starting codex node";

        public void Process(CodexLogLine line, Action<Action<OverwatchCodexEvent>> addEvent)
        {
            // "(
            // configFile: none(InputFile),
            // logLevel: \"TRACE;warn:discv5,providers,manager,cache;warn:libp2p,multistream,switch,transport,tcptransport,semaphore,asyncstreamwrapper,lpstream,mplex,mplexchannel,noise,bufferstream,mplexcoder,secure,chronosstream,connection,connmanager,websock,ws-session,dialer,muxedupgrade,upgrade,identify;warn:contracts,clock;warn:serde,json,serialization\",
            // logFormat: auto,
            // metricsEnabled: false,
            // metricsAddress: 127.0.0.1,
            // metricsPort: 8008,
            // dataDir: datadir5,
            // circuitDir: /root/.cache/codex/circuits,
            // listenAddrs: @[/ip4/0.0.0.0/tcp/8081],
            // nat: 10.1.0.214,
            // discoveryIp: 0.0.0.0,
            // discoveryPort: 8080,
            // netPrivKeyFile: \"key\",
            // bootstrapNodes:
                // @[(envelope: (publicKey: secp256k1 key (0414380858330307a4a59e8a1643512f80680dedb8c541674f40382be71c26556cd65b7e1775ec9fabfaf6d58d562b88a3c8afb969f8cc256db20e4c4c9e1f70a6),
                // domain: \"libp2p-peer-record\",
                // payloadType: @[3, 1],
                // payload: @[10, 39, 0, 37, 8, 2, 18, 33, 2, 20, 56, 8, 88, 51, 3, 7, 164, 165, 158, 138, 22, 67, 81, 47, 128, 104, 13, 237, 184, 197, 65, 103, 79, 64, 56, 43, 231, 28, 38, 85, 108, 16, 254, 211, 141, 181, 6, 26, 11, 10, 9, 4, 10, 1, 0, 210, 145, 2, 31, 144],
                // signature: 3045022100FA846871D96EDCA579990244B1C590E16B57A7BDB817908A2B580043F8A0B0280220342825DBE577E83C22CD59AA9099DE8F8DC86A38C659F60E8B9255126CF37FDE),
                // data:
                    // (peerId: 16Uiu2HAkvnbgwdB2NmNe1uWGJxE3Ep3sDHxNW95W2rTPs2vfxvou,
                    // seqNo: 1721985534,
                    // addresses: @[(address: /ip4/10.1.0.210/udp/8080)]
                    // )
                // )],
            // maxPeers: 160,
            // agentString: \"Codex\",
            // apiBindAddress: \"0.0.0.0\",
            // apiPort: 30035,
            // apiCorsAllowedOrigin: none(string),
            // repoKind: fs,
            // storageQuota: 8589934592\'NByte,
            // blockTtl: 1d,
            // blockMaintenanceInterval: 10m,
            // blockMaintenanceNumberOfBlocks: 1000,
            // cacheSize: 0\'NByte,
            // logFile: none(string),
            // cmd: noCmd
            // )"

            var config = line.Attributes["config"];
            
            while (config.Contains(peerIdTag))
            {
                var openIndex = config.IndexOf(peerIdTag) + peerIdTag.Length;
                var closeIndex = config.IndexOf(",", openIndex);
                var bootPeerId = config.Substring(openIndex, closeIndex - openIndex);
                config = config.Substring(closeIndex);

                addEvent(e =>
                {
                    e.BootstrapConfig = new BootstrapConfigEvent
                    {
                        BootstrapPeerId = bootPeerId
                    };
                });
            }
        }
    }
}
