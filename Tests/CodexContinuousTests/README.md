# Codex Continuous Tests
This CLI tool runs tests in an endless loop, using a network of Codex nodes in a kubernetes cluster. Run `dotnet run -- --help` to view all CLI options.

## Choosing tests
By default, all tests in the `CodexContinuousTests/Tests` folder will be used. If you want to limit your test run to a subset of tests, please delete or disable the other test code files. TODO: We'd like a CLI option for selecting tests. Similar to `dotnet test --filter`, maybe?

## Where do I get a `codex-deployment.json`
See [THIS](../../Tools/CodexNetDeployer/README.MD)

## Output
The test runner will produce a folder with all the test logs. They are sorted by timestamp and reflect the names of the tests. When a test fails, the log file for that specific test will be postfixed with `_FAILED`.

### Pass and fail conditions
While individual tests can pass or fail for a number of times and/or over a length of time as configurable with the CLI argument, the test run entirely is not considered passed or failed until either of the following conditions are met:
1. Failed: The number of test failures has reached the specifid number, or the test runner was manually cancelled.
1. Passed: The failed condition was not reached within the time specified by the target-duration option.

## Transient nodes
The continuous tests runner is designed to use a network of Codex nodes deployed in a kubernetes cluster. The runner will not influence or manage the lifecycle of the nodes in this deployment. However, some test cases require transient nodes.
A transient node is a node started and managed by the test runner on behalf of a specific test. The runner facilitates the tests to start and stop transient nodes, as well as bootstrap those nodes against (permanent) nodes from the deployed network. The test runner makes sure that the transient nodes use the same docker image as the permanent nodes, to avoid version conflicts. However, the image used for transient nodes can be overwritten by setting the `CODEXDOCKERIMAGE` environment variable. The use of a local Codex repository for building override images is not supported for transient nodes.
