# Continuous Tests

 1. [Description](#description)
 2. [Prerequisites](#prerequisites)
 3. [Run tests](#run-tests)
 4. [Analyze logs](#analyze-logs)


## Description

 Continuous Tests were developed to perform long lasting tests in different configurations and topologies. Unlike Distributed Tests, they are running continuously, until we stop them manually. Such approach is very useful to detect the issues which may appear over the time when we may have blocking I/O, unclosed pools/connections and etc.

 Usually, we are running Continuous Tests manually and for automated runs, please refer to the [Tests automation](Automation.md).

 We have two projects in the repository
 - [CodexNetDeployer](../CodexNetDeployer) - Prepare environment to run the tests
 - [ContinuousTests](../ContinuousTests) - Continuous Tests

 And they are used to prepare environment and run Continuous Tests.


## Prerequisites

 1. Kubernetes cluster, to run the tests
 2. kubeconfig file, to access the cluster
 3. [kubectl](https://kubernetes.io/docs/tasks/tools/) installed, to create resources in the cluster
 4. Optional - [OpenLens](https://github.com/MuhammedKalkan/OpenLens) installed, to browse cluster resources


## Run tests
 1. Create a Pod in the cluster, in the `default` namespace and consider to use your own value for `metadata.name`
    <details>
    <summary>tests-runner.yaml</summary>

    ```yaml
    ---
    apiVersion: v1
    kind: Pod
    metadata:
      name: tests-runner
      namespace: default
      labels:
        name: manual-run
    spec:
      containers:
      - name: runner
        image: mcr.microsoft.com/dotnet/sdk:7.0
        env:
        - name: KUBECONFIG
          value: /opt/kubeconfig.yaml
      #   volumeMounts:
      #   - name: kubeconfig
      #     mountPath: /opt/kubeconfig.yaml
      #     subPath: kubeconfig.yaml
      #   - name: logs
      #    mountPath: /var/log/codex-dist-tests
        command: ["sleep", "infinity"]
      # volumes:
      #   - name: kubeconfig
      #     secret:
      #       secretName: codex-dist-tests-app-kubeconfig
      #   - name: logs
      #     hostPath:
      #       path: /var/log/codex-dist-tests
    ```

    ```shell
    kubectl apply -f tests-runner.yaml
    ```

 2. Copy kubeconfig to the runner Pod using the name you set in the previous step
    ```shell
    kubectl cp ~/.kube/codex-dist-tests.yaml tests-runner:/opt/kubeconfig.yaml
    ```

 3. Exec into the runner Pod using the name you set in the previous step
    ```shell
    # kubectl
    kubectl exec -it tests-runner -- bash

    # OpenLens
    OpenLens --> Pods --> dist-tests-runner --> "Press on it" --> Pod Shell
    ```

 4. Install required packages
    ```shell
    apt update
    apt install -y tmux
    ```

 5. Clone Continuous Tests repository
    ```shell
    tmux

    cd /opt
    git clone https://github.com/codex-storage/cs-codex-dist-tests.git
    ```

 6. Run `CodexNetDeployer`
    ```shell
    # Usually take ~ 10 minutes
    cd cs-codex-dist-tests/CodexNetDeployer

    export RUNID=$(date +%Y%m%d-%H%M%S)
    bash deploy-continuous-testnet.sh
    ```

 7. Run `ContinuousTests`
    ```
    cd ../ContinuousTests
    cp ../CodexNetDeployer/codex-deployment.json .

    bash run.sh
    ```


## Analyze logs

 We should check the logs in the `/opt/cs-codex-dist-tests/ContinuousTests/logs` folder
