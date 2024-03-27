# CodexNetDeployer

This CLI tool allows you to create a network of Codex nodes in a kubernetes cluster. There are many arguments that allow you to configure every aspect of the deployment.  Each argument can also be controlled using an environment variable. I won't copy-paste the list here because then it'll have to be kept up-to-date. Just run:
`dotnet run -- --help`

## Output
After the deployment has successfully finished, a `codex-deployment.json` file will be created. This file contains all the information necessary to interface with the deployed network. It can be used by other tools, for example the CodexContinuousTests runner CLI tool.

## Overrides
The arguments allow you to configure quite a bit, but not everything. Here are some environment variables the CodexNetDeployer will respond to. None of these are required.

| Variable         | Description                                                                                                    |
|------------------|----------------------------------------------------------------------------------------------------------------|
| DEPLOYID         | A pod-label 'deployid' is added to each pod created during the tests. Use this to set the value of that label. |
| TESTID           | Similar to RUNID, except the label is 'testid'.                                                                |
| CODEXDOCKERIMAGE | If set, this will be used instead of the default Codex docker image.                                           |

## Using a local Codex repository
If you have a clone of the Codex git repository, and you want to deploy a network using your local modifications, the following environment variable options are for you. Please note that any changes made in Codex's 'vendor' directory will be discarded during the build process.

| Variable       | Description                                                                                                              |
|----------------|--------------------------------------------------------------------------------------------------------------------------|
| CODEXREPOPATH  | Path to the Codex repository.                                                                                            |
| DOCKERUSERNAME | Username of your Dockerhub account.                                                                                      |
| DOCKERPASSWORD | Password OR access-token for your Dockerhub account. You can omit this variable to use your system-default account.      |
| DOCKERTAG      | Optional. Tag used for docker image that will be built and pushed to the Dockerhub account. Random ID used when not set. |
