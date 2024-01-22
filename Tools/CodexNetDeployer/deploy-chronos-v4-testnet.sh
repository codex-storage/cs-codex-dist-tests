#!/usr/bin/env bash

export DEPLOYMENT_NAME="-chronos-v4"
export DEPLOYMENT_NAMESPACE="-chronos-v4-testnet"
export CODEX_STORAGE_QUOTA=60000
export CODEX_BLOCK_TTL=9999999
export CODEX_BLOCK_MI=9999999
export DEPLOYMENT_NODES=5
export DEPLOYMENT_VALIDATORS=3

. ./deploy-continuous-testnet.sh