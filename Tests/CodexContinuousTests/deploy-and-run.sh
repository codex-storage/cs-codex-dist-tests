set -e

replication=5
name=testnamehere
filter=TwoClient

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
    --metrics=1 \
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
    --log-path=logs-$name-$i \
    --data-path=data-$name-$i \
    --keep=1 \
    --stop=1 \
    --filter=$filter \
    --cleanup=1 \
    --full-container-logs=1 \
    --target-duration=172800 # 48 hours

    sleep 30
done
