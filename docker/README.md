# Run tests with Docker in Kubernetes

 We may [run tests on local](../LOCALSETUP.MD) or remote Kubernetes cluster. Local cluster flow uses direct access to nodes ports and this is why we introduced a different way to check services ports, for more information please see [Tests run modes](../../../issues/20). Configuration option `RUNNERLOCATION` is responsible for that.


 For local run it is easier to install .Net and run tests on Docker Desktop Kubernetes cluster. In case of remote run we do not expose services via Ingress Controller and we can't access cluster nodes, this is why we should run tests only inside the Kubernetes.

 We can run tests on remote cluster in the following ways

#### Run pod inside the cluster using generic .Net image

<details>
<summary>steps</summary>

1. Create dist-tests-runner.yaml
   ```yaml
      --
      apiVersion: v1
      kind: Pod
      metadata:
      name: dist-tests-runner
      namespace: default
      spec:
      containers:
      - name: dotnet
         image: mcr.microsoft.com/dotnet/sdk:7.0
         command: ["sleep", "infinity"]
   ```
2. Deploy pod in the cluster
   ```shell
   kubectl apply -f dist-tests-runner.yaml
   ```

3. Copy kubeconfig to the pod
   ```shell
   kubectl cp kubeconfig.yaml dist-tests-runner:/opt
   ```

4. Exec into the pod via kubectl or [OpenLens](https://github.com/MuhammedKalkan/OpenLens)
   ```shell
   kubectl exec -it dist-tests-runner -- bash
   ```

5. Clone repository inside the pod
   ```shell
   git clone https://github.com/codex-storage/cs-codex-dist-tests.git
   ```

6. Update kubeconfig option in config file
   ```shell
   cd cs-codex-dist-tests
   vi DistTestCore/Configuration.cs
   ```
   ```dotnet
   GetNullableEnvVarOrDefault("KUBECONFIG", "/opt/kubeconfig.yaml")
   ```

7. Run tests
   ```shell
   dotnet test Tests
   ```

8. Check the results and analyze the logs
</details>

#### Run pod inside the cluster using [prepared Docker images](https://hub.docker.com/r/codexstorage/cs-codex-dist-tests/tags)

 Before the run we should create some objects inside the cluster
 1. Namespace where we will run the image
 2. [Service Account to run tests inside the cluster](https://github.com/codex-storage/cs-codex-dist-tests/issues/21)
 3. Secret with kubeconfig for created SA
 4. Configmap with custom app config if required

 For more information please see [Manual run inside Kubernetes via Job](../../../issues/7)

 Then we need to create a manifest to run the pod
 <details>
 <summary>runner.yaml</summary>

 ```yaml
 ---
 apiVersion: v1
 kind: Pod
 metadata:
   name: dist-tests-runner
   namespace: cs-codex-dist-tests
   labels:
     name: cs-codex-dist-tests
 spec:
   containers:
   - name: cs-codex-dist-tests
     image: codexstorage/cs-codex-dist-tests:sha-671ee4e
     env:
     - name: RUNNERLOCATION
       value: InternalToCluster
     - name: KUBECONFIG
       value: /opt/kubeconfig.yaml
     - name: CONFIG
       value: "/opt/Configuration.cs"
     - name: CONFIG_SHOW
       value: "true"
     volumeMounts:
     - name: kubeconfig
       mountPath: /opt/kubeconfig.yaml
       subPath: kubeconfig.yaml
     - name: config
       mountPath: /opt/Configuration.cs
       subPath: Configuration.cs
     - name: logs
       mountPath: /var/log/cs-codex-dist-tests
     # command:
     # - "dotnet"
     # - "test"
     # - "Tests"
   restartPolicy: Never
   volumes:
     - name: kubeconfig
       secret:
         secretName: cs-codex-dist-tests-app-kubeconfig
     - name: config
       configMap:
         name: cs-codex-dist-tests
     - name: logs
       hostPath:
         path: /var/log/cs-codex-dist-tests
 ```
 For more information about pod variables please see [job.yaml](job.yaml).
 </details>

 And then apply it
 ```shell
 kubectl apply -f runner.yaml
 ```

 After the pod run, custom [entrypoint](docker-entrypoint.sh) will do the following
 1. Clone repository
 2. Switch to the specific branch - `master` by default
 3. Run all tests - `dotnet test`
