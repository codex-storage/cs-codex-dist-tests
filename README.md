# Distributed System Tests for Nim-Codex

Using a common dotnet unit-test framework and a few other libraries, this project allows you to write tests that use multiple Codex node instances in various configurations to test the distributed system in a controlled, reproducible environment.

Nim-Codex: https://github.com/codex-storage/nim-codex  
Dotnet: v7.0  
Kubernetes: v1.25.4  
Dotnet-kubernetes SDK: v10.1.4 https://github.com/kubernetes-client/csharp  
Nethereum: v4.14.0

## Tests/CodexTests and Tests/CodexLongTests
These are test assemblies that use NUnit3 to perform tests against transient Codex nodes.
Read more [HERE](/Tests/CodexTests/README.md)

## Tests/ContinuousTests
A console application that runs tests in an endless loop against a persistent deployment of Codex nodes.
Read more [HERE](/Tests/CodexContinuousTests/README.md)

## Tools/CodexNetDeployer
A console application that can deploy Codex nodes.
Read more [HERE](/Tools/CodexNetDeployer/README.MD)

## Framework architecture
The framework is designed to be extended by project-specific plugins. These plugins contribute functionality and abstractions to the framework. Users of the framework use these to perform tasks such as testing and deploying.
![Architecture](/docs/FrameworkArchitecture.png)

## How to contribute a plugin
If you want to add support for your project to the testing framework, follow the steps [HERE](/CONTRIBUTINGPLUGINS.MD)

## How to contribute tests
If you want to contribute tests, please follow the steps [HERE](/CONTRIBUTINGTESTS.md).

## Run the tests on your machine
Creating tests is much easier when you can debug them on your local system. This is possible, but requires some set-up. If you want to be able to run the tests on your local system, follow the steps [HERE](/docs/LOCALSETUP.md). Please note that tests which require explicit node locations cannot be executed locally. (Well, you could comment out the location statements and then it would probably work. But that might impact the validity/usefulness of the test.)

## Missing functionality
Surely the test-infra doesn't do everything we'll need it to do. If you're running into a limitation and would like to request a new feature for the test-infra, please create an issue.
