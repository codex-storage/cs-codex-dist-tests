# Distributed System Tests for Nim-Codex

Nim-Codex: https://github.com/codex-storage/nim-codex

Tests are built on dotnet v6.0 and Kubernetes v1.25.4, using dotnet-kubernetes SDK: https://github.com/kubernetes-client/csharp

## Requirement

At this moment, the tests require a local kubernetes cluster to be installed.

## Run

Short tests: These tests may take minutes to an hour.
`dotnet test Tests`

Long tests: These may takes hours to days.
`dotnet test LongTests`

