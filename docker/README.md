# Run tests with Docker in Kubernetes

We may [run tests localy](../LOCALSETUP.MD) using installed Dotnet and inside Kubernetes we may use a [prepared Docker images](https://hub.docker.com/r/codexstorage/cs-codex-dist-tests/tags).


Custom [entrypoint](docker-entrypoint.sh) will do the following
 1. Clone repository
 2. Switch to the specific branch - `master` by default
 3. Run all tests - `dotnet test`

**Run with defaults**
```bash
docker run \
  --rm \
  --name cs-codex-dist-tests \
  codexstorage/cs-codex-dist-tests:sha-686757e
```

**Just short tests**
```bash
docker run \
  --rm \
  --name cs-codex-dist-tests \
  codexstorage/cs-codex-dist-tests:sha-686757e \
  dotnet test Tests
```

**Custom branch**
```bash
docker run \
  --rm \
  --name cs-codex-dist-tests \
  --env BRANCH=feature/tests \
  codexstorage/cs-codex-dist-tests:sha-686757e
```

**Custom local config**
```bash
docker run \
  --rm \
  --name cs-codex-dist-tests \
  --env CONFIG=/opt/Configuration.cs \
  --env CONFIG_SHOW=true \
  --volume $PWD/DistTestCore/Configuration.cs:/opt/Configuration.cs \
  codexstorage/cs-codex-dist-tests:sha-686757e
```

**Local kubeconfig with custom local config**
```bash
docker run \
  --rm \
  --name cs-codex-dist-tests \
  --env CONFIG=/opt/Configuration.cs \
  --env CONFIG_SHOW=true \
  --env SOURCE=https://github.com/codex-storage/cs-codex-dist-tests.git \
  --volume $PWD/DistTestCore/Configuration.cs:/opt/Configuration.cs \
  --volume $PWD/kubeconfig.yml:/opt/kubeconfig.yml \
  codexstorage/cs-codex-dist-tests:sha-686757e
```
