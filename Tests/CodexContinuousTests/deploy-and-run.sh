set -e

replication=5

echo "Deploying..."
cd ../../Tools/CodexNetDeployer
for i in $( seq 0 $replication)
do
    dotnet run \
    --kube-config=/opt/kubeconfig.yaml \
    --kube-namespace=codex-continuous-tests-$i \
    --deploy-file=codex-deployment-$i.json \
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
    --metrics=1 \
    --check-connect=1 \
    -y

    cp codex-deployment-$i.json ../../Tests/CodexContinuousTests
done
echo "Starting tests..."
cd ../../Tests/CodexContinuousTests
for i in $( seq 0 $replication)
do
    screen -d -m dotnet run \
    --kube-config=/opt/kubeconfig.yaml \
    --codex-deployment=codex-deployment-$i.json \
    --log-path=logs-$i \
    --data-path=data-$i \
    --keep=1 \
    --stop=1 \
    --filter=TwoClient \
    --cleanup=1 \
    --full-container-logs=1 \
    --target-duration=172800 # 48 hours
done
