#!/bin/bash

# Variables
## Common
SOURCE="${SOURCE:-https://github.com/codex-storage/cs-codex-dist-tests.git}"
BRANCH="${BRANCH:-master}"
FOLDER="${FOLDER:-/opt/cs-codex-dist-tests}"

## Tests specific
DEPLOYMENT_CODEXNETDEPLOYER_PATH="${DEPLOYMENT_CODEXNETDEPLOYER_PATH:-Tools/CodexNetDeployer}"
DEPLOYMENT_CODEXNETDEPLOYER_RUNNER="${DEPLOYMENT_CODEXNETDEPLOYER_RUNNER:-deploy-continuous-testnet.sh}"
CONTINUOUS_TESTS_FOLDER="${CONTINUOUS_TESTS_FOLDER:-Tests/CodexContinuousTests}"
CONTINUOUS_TESTS_RUNNER="${CONTINUOUS_TESTS_RUNNER:-run.sh}"

# Get code
echo -e "Cloning ${SOURCE} to ${FOLDER}\n"
git clone -b "${BRANCH}" "${SOURCE}" "${FOLDER}"
echo -e "\nChanging folder to ${FOLDER}\n"
cd "${FOLDER}"

# Run tests
echo -e "Running tests from branch '$(git branch --show-current) ($(git rev-parse --short HEAD))'\n"

if [[ "${TESTS_TYPE}" == "continuous-tests" ]]; then
  echo -e "Running CodexNetDeployer\n"
  bash "${DEPLOYMENT_CODEXNETDEPLOYER_PATH}"/"${DEPLOYMENT_CODEXNETDEPLOYER_RUNNER}"
  echo
  echo -e "Running continuous-tests\n"
  bash "${CONTINUOUS_TESTS_FOLDER}"/"${CONTINUOUS_TESTS_RUNNER}"
else
  echo -e "Running ${TESTS_TYPE}\n"
  exec "$@"
fi
