#!/bin/bash
echo "Running continuous tests..."
cd /app/Tests/CodexContinuousTests
exec "$@"

