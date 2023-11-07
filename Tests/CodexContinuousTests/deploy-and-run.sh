#!/usr/bin/env bash
set -e

kubeconfig="/opt/kubeconfig.yaml"
replication=5
name=profiling-two-client-tests
filter=TwoClient
cleanup=1

usage() { 
    echo "Usage: $0 [-m <deploy|run>] [-r <replication>] [-k <kubeconfig>] [-c <cleanup>]" 1>&2
    exit 1
}

while getopts ":r:k:m:c:" o; do
    case "$o" in
        r)
            replication="$OPTARG"
            ;;
        k)
            kubeconfig="$OPTARG"
            ;;
        m)
            mode="$OPTARG"
            ;;
        c)
            cleanup="$OPTARG"
            ;;
        *)
            usage
            ;;
    esac
done

echo "Replication: $replication; Kube config: $kubeconfig; Mode: $mode; Cleanup: $cleanup"

if [ -z "$mode" ] || [ "$mode" == "deploy" ]; then
    echo "Deploying..."
    cd ../../Tools/CodexNetDeployer
    for i in $(seq 0 "$replication")
    do
        dotnet run \
        --deploy-name=codex-continuous-"$name-$i" \
        --kube-config="$kubeconfig" \
        --kube-namespace=codex-continuous-tests-"$name-$i" \
        --deploy-file=codex-deployment-"$name-$i".json \
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

        cp codex-deployment-"$name-$i".json ../../Tests/CodexContinuousTests
    done
    exit 0
fi

if [ -z "$mode" ] || [ "$mode" == "run" ]; then
    echo "Starting tests..."
    cd ../../Tests/CodexContinuousTests
    for i in $(seq 0 "$replication")
    do
        screen -d -m dotnet run \
        --kube-config="${kubeconfig}" \
        --codex-deployment=codex-deployment-"$name-$i".json \
        --log-path=logs-"$i" \
        --data-path=data-"$i" \
        --keep=1 \
        --stop=1 \
        --filter="$filter" \
        --cleanup="$cleanup" \
        --full-container-logs=1 \
        --target-duration=172800 # 48 hours
    done
fi
