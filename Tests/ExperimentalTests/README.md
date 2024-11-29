# Codex Tests
This is an NUnit test assembly that can be used with the standard dotnet test runner. For all its CLI options, run `dotnet test --help`.

## Example tests
Running all the tests in the assembly can take a while. In order to check basic viability of your setup as well as the Codex image you're using, consider running only the example tests using the filter option: `dotnet test --filter=Example`.

## Output
The test runner will produce a folder named `CodexTestLogs` with all the test logs. They are sorted by timestamp and reflect the names of the test fixtures and individual tests. When a test fails, the log file for that specific test will be postfixed with `_FAILED`. The same applies to the fixture log file. The `STATUS` files contain the test results in JSON, for easy machine reading.

## Overrides
The following environment variables allow you to override specific aspects of the behaviour of the tests.

| Variable         | Description                                                                                                    |
|------------------|----------------------------------------------------------------------------------------------------------------|
| DEPLOYID         | A pod-label 'deployid' is added to each pod created during the tests. Use this to set the value of that label. |
| TESTID           | Similar to RUNID, except the label is 'testid'.                                                                |
| CODEXDOCKERIMAGE | If set, this will be used instead of the default Codex docker image.                                           |

## Using a local Codex repository
If you have a clone of the Codex git repository, and you want to run the tests using your local modifications, the following environment variable options are for you. Please note that any changes made in Codex's 'vendor' directory will be discarded during the build process.

| Variable       | Description                                                                                                              |
|----------------|--------------------------------------------------------------------------------------------------------------------------|
| CODEXREPOPATH  | Path to the Codex repository.                                                                                            |
| DOCKERUSERNAME | Username of your Dockerhub account.                                                                                      |
| DOCKERPASSWORD | Password OR access-token for your Dockerhub account. You can omit this variable to use your system-default account.      |
| DOCKERTAG      | Optional. Tag used for docker image that will be built and pushed to the Dockerhub account. Random ID used when not set. |
