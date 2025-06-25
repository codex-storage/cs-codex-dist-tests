#!/bin/bash

# Marketplace address from URL
if [[ -n "${MARKETPLACE_ADDRESS_FROM_URL}" ]]; then
  WAIT=${MARKETPLACE_ADDRESS_FROM_URL_WAIT:-300}
  SECONDS=0
  SLEEP=1
  # Run and retry if fail
  while (( SECONDS < WAIT )); do
    MARKETPLACE_ADDRESS=($(curl -s -f -m 5 "${MARKETPLACE_ADDRESS_FROM_URL}"))
    # Check if exit code is 0 and returned value is not empty
    if [[ $? -eq 0 && -n "${MARKETPLACE_ADDRESS}" ]]; then
      export CODEXCONTRACTS_MARKETPLACEADDRESS="${MARKETPLACE_ADDRESS}"
      break
    else
      # Sleep and check again
      echo "Can't get Marketplace address from ${MARKETPLACE_ADDRESS_FROM_URL} - Retry in $SLEEP seconds / $((WAIT - SECONDS))"
      sleep $SLEEP
    fi
  done
fi

# Show
echo -e "\nRun parameters:"
vars=$(env | grep "CODEX" | grep -v -e "[0-9]_SERVICE_" -e "[0-9]_NODEPORT_")
echo -e "${vars//CODEX/   - CODEX}"
echo -e "   - $@\n"

# Run
exec "$@"
