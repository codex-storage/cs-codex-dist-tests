set -e

replication=$DNR_REP
name=$DNR_NAME
filter=$DNR_FILTER
duration=$DNR_DURATION

echo "Deploying..."
cd ../../Tools/CodexNetDeployer
for i in $( seq 0 $replication)
do
    dotnet run \
    --deploy-name=codex-continuous-$name-$i \
    --kube-config=/opt/kubeconfig.yaml \
    --kube-namespace=codex-continuous-$name-tests-$i \
    --deploy-file=codex-deployment-$name-$i.json \
    --nodes=5 \
    --validators=3 \
    --log-level=Trace \
    --storage-quota=20480 \
    --storage-sell=1024 \
    --min-price=1024 \
    --max-collateral=1024 \
    --max-duration=3600000 \
    --block-ttl=99999999 \
    --block-mi=99999999 \
    --block-mn=100 \
    --metrics-endpoints=1 \
    --metrics-scraper=1 \
    --check-connect=1 \
    -y

    cp codex-deployment-$name-$i.json ../../Tests/CodexContinuousTests
done
echo "Starting tests..."
cd ../../Tests/CodexContinuousTests
for i in $( seq 0 $replication)
do
    screen -d -m dotnet run \
    --kube-config=/opt/kubeconfig.yaml \
    --codex-deployment=codex-deployment-$name-$i.json \
    --log-path=/var/log/codex-continuous-tests/logs-$name-$i \
    --data-path=data-$name-$i \
    --keep=1 \
    --stop=1 \
    --filter=$filter \
    --cleanup=1 \
    --full-container-logs=1 \
    --target-duration=$duration

    sleep 30
done

echo "Done! Sleeping indefinitely..."
while true; do sleep 1d; done
