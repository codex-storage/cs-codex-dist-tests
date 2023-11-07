dotnet run \
    --project "${DEPLOYMENT_CODEXNETDEPLOYER_PATH:-Tools/CodexNetDeployer}" \
    --deploy-name="${DEPLOYMENT_NAME:-codex-continuous-test-deployment}" \
    --kube-config="${KUBECONFIG:-/opt/kubeconfig.yaml}" \
    --kube-namespace="${DEPLOYMENT_NAMESPACE:-codex-continuous-tests}" \
    --deploy-file="${DEPLOYMENT_FILE:-codex-deployment.json}" \
    --nodes=${DEPLOYMENT_NODES:-5} \
    --validators=${DEPLOYMENT_VALIDATORS:-3} \
    --log-level="${CODEX_LOG_LEVEL:-Trace}" \
    --storage-quota=${CODEX_STORAGE_QUOTA:-2048} \
    --storage-sell=${CODEX_STORAGE_SELL:-1024} \
    --min-price=${CODEX_MIN_PRICE:-1024} \
    --max-collateral=${CODEX_MAX_COLLATERAL:-1024} \
    --max-duration=${CODEX_MAX_DURATION:-3600000} \
    --block-ttl=${CODEX_BLOCK_TTL:-180} \
    --block-mi=${CODEX_BLOCK_MI:-120} \
    --block-mn=${CODEX_BLOCK_MN:-10000} \
    --metrics=${CODEX_METRICS:-1} \
    --check-connect=${DEPLOYMENT_CHECK_CONNECT:-1} \
    -y
