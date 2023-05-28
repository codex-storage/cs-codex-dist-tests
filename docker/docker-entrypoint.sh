#!/bin/bash

# Variables
SOURCE="${SOURCE:-https://github.com/codex-storage/cs-codex-dist-tests.git}"
BRANCH="${BRANCH:-master}"
FOLDER="${FOLDER:-/opt/dist-tests}"


# Get tests
echo "Clone ${SOURCE}"
git clone -b "${BRANCH}" "${SOURCE}" "${FOLDER}"
[[ -n "${CONFIG}" ]] && { echo Link config "${CONFIG}"; ln --symbolic --force "${CONFIG}" "${FOLDER}/DistTestCore/Configuration.cs"; }
[[ "${CONFIG_SHOW}" == "true" ]] && { echo Show config "${CONFIG}"; cat "${FOLDER}/DistTestCore/Configuration.cs"; }
cd "${FOLDER}"

# Run
echo "Run tests on branch '`git branch --show-current`' ..."
exec "$@"

