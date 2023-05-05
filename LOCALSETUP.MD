# Distributed System Tests for Nim-Codex

## Local setup
These steps will help you set up everything you need to run and debug the tests on your local system.

### Installing the requirements.
1. Install dotnet v6.0 or newer. (If you install a newer version, consider updating the .csproj files by replacing all mention of `net6.0` with your version.)
1. Set up a nice C# IDE or plugin for your current IDE.
1. Install docker desktop.
1. In the docker-desktop settings, enable kubernetes. (This might take a few minutes.)

### Configure to taste.
The tests should run as-is. You can change the configuration. The items below explain the what and how.
1. Open `DistTestCore/Configuration.cs`.
1. `k8sNamespace` defines the Kubernetes namespace the tests will use. All Kubernetes resources used during the test will be created in it. At the beginning of a test run and at the end of each test, the namespace and all resources in it will be deleted.
1. `kubeConfigFile`. If you are using the Kubernetes cluster created in docker desktop, this field should be set to null. If you wish to use a different cluster, set this field to the path (absolute or relative) of your KubeConfig file.
1. `LogConfig(path, debugEnabled)`. Path defines the path (absolute or relative) where the tests logs will be saved. The debug flag allows you to enable additional logging. This is mostly useful when something's wrong with the test infra.
1. `FileManagerFolder` defines the path (absolute or relative) where the test infra will generate and store test data files. This folder will be deleted at the end of every test run.

### Running the tests
Most IDEs will let you run individual tests or test fixtures straight from the code file. If you want to run all the tests, you can use `dotnet test`. You can control which tests to run by specifying which folder of tests to run. `dotnet test Tests` will run only the tests in `/Tests` and exclude the long tests.
