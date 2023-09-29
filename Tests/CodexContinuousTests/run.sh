dotnet run \
    --kube-config=/opt/kubeconfig.yaml \
    --codex-deployment=codex-deployment.json \
    --keep=1 \
    --stop=10 \
    --target-duration=172800 # 48 hours
