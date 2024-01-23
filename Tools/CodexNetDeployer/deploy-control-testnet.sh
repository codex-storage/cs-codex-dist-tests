export DEPLOYMENT_NAME="-chronos-v4-control"
export DEPLOYMENT_NAMESPACE="chronos-v4-control"
export CODEX_STORAGE_QUOTA=60000
export CODEX_BLOCK_TTL=9999999
export CODEX_BLOCK_MI=9999999
export DEPLOYMENT_NODES=5
export DEPLOYMENT_VALIDATORS=3
export DEPLOYMENT_FILE="codex-chronos-v4-control-deployment.json"

REL_PATH=$(dirname "${BASH_SOURCE[0]}")

. "${REL_PATH}/deploy-continuous-testnet.sh"