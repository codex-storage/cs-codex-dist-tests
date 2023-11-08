#!/bin/bash

# Common
SOURCE="${SOURCE:-https://github.com/codex-storage/cs-codex-dist-tests.git}"
BRANCH="${BRANCH:-master}"
FOLDER="${FOLDER:-/opt/cs-codex-dist-tests}"

# Continuous Tests
DEPLOYMENT_CODEXNETDEPLOYER_PATH="${DEPLOYMENT_CODEXNETDEPLOYER_PATH:-Tools/CodexNetDeployer}"
DEPLOYMENT_CODEXNETDEPLOYER_RUNNER="${DEPLOYMENT_CODEXNETDEPLOYER_RUNNER:-deploy-continuous-testnet.sh}"
CONTINUOUS_TESTS_FOLDER="${CONTINUOUS_TESTS_FOLDER:-Tests/CodexContinuousTests}"
CONTINUOUS_TESTS_RUNNER="${CONTINUOUS_TESTS_RUNNER:-run.sh}"


# Get code
echo "`date` - Clone ${SOURCE}"
git clone -b "${BRANCH}" "${SOURCE}" "${FOLDER}"
echo "`date` - Change folder to ${FOLDER}"
cd "${FOLDER}"

# Run
echo "Run tests from branch '`git branch --show-current` / `git rev-parse HEAD`'"

if [[ "${TESTS_TYPE}" == "continuous-tests" ]]; then
  echo "`date` - Running Continuous Tests"
  echo
  echo "`date` - Running CodexNetDeployer"
  bash "${DEPLOYMENT_CODEXNETDEPLOYER_PATH}"/"${DEPLOYMENT_CODEXNETDEPLOYER_RUNNER}"
  echo
  echo "`date` - Running Tests"
  bash "${CONTINUOUS_TESTS_FOLDER}"/"${CONTINUOUS_TESTS_RUNNER}"
else
  echo "`date` - Running Dist Tests"
  exec "$@"
fi
