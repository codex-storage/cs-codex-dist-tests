# Distributed System Tests for Nim-Codex

Using a common dotnet unit-test framework and a few other libraries, this project allows you to write tests that use multiple Codex node instances in various configurations to test the distributed system in a controlled, reproducible environment.

Nim-Codex: https://github.com/codex-storage/nim-codex  
Dotnet: v7.0  
Kubernetes: v1.25.4  
Dotnet-kubernetes SDK: v10.1.4 https://github.com/kubernetes-client/csharp  
Nethereum: v4.14.0

## Tests/CodexTests and Tests/CodexLongTests
These are test assemblies that use NUnit3 to perform tests against transient Codex nodes.

## Tests/ContinousTests
A console application that runs tests in an endless loop against a persistent deployment of Codex nodes.

## Tools/CodexNetDeployer
A console application that can deploy Codex nodes.

## Test logs
Because tests potentially take a long time to run, logging is in place to help you investigate failures afterwards. Should a test fail, all Codex terminal output (as well as metrics if they have been enabled) will be downloaded and stored along with a detailed, step-by-step log of the test. If something's gone wrong and you're here to discover the details, head for the logs.

## How to contribute a plugin
If you want to add support for your project to the testing framework, follow the steps [HERE](/CONTRIBUTINGPLUGINS.MD)

## How to contribute tests
If you want to contribute tests, please follow the steps [HERE](/CONTRIBUTINGTESTS.md).

## Run the tests on your machine
Creating tests is much easier when you can debug them on your local system. This is possible, but requires some set-up. If you want to be able to run the tests on your local system, follow the steps [HERE](/docs/LOCALSETUP.md). Please note that tests which require explicit node locations cannot be executed locally. (Well, you could comment out the location statements and then it would probably work. But that might impact the validity/usefulness of the test.)

## Missing functionality
Surely the test-infra doesn't do everything we'll need it to do. If you're running into a limitation and would like to request a new feature for the test-infra, please create an issue.
