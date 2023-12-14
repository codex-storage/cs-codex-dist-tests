dotnet run \
    --deploy-name=codex-public-testnet \
    --kube-config=/opt/kubeconfig.yaml \
    --kube-namespace=codex-public-testnet \
    --deploy-file=codex-public-testnet-deployment.json \
    --nodes=3 \
    --validators=1 \
    --log-level=Trace \
    --storage-quota=2048 \
    --make-storage-available=0 \
    --block-ttl=180 \
    --block-mi=120 \
    --block-mn=10000 \
    --metrics-endpoints=1 \
    --metrics-scraper=0 \
    --check-connect=0 \
\
    --public-testnet=1 \
    --public-discports=30010,30020,30030 \
    --public-listenports=30011,30021,30031 \
    --public-gethdiscport=30040 \
    --public-gethlistenport=30041 \
\
    --discord-bot=1 \
    --dbot-token=tokenhere \
    --dbot-servername=namehere \
    --dbot-adminrolename=alsonamehere \
    --dbot-adminchannelname=channelname \
    --dbot-datapath=/var/botdata
