# Distributed System Tests for Nim-Codex

## Local setup
These steps will help you set up everything you need to run and debug the tests on your local system.

### Installing the requirements.
1. Install dotnet v7.0 or newer. (If you install a newer version, consider updating the .csproj files by replacing all mention of `net7.0` with your version.)
1. Set up a nice C# IDE or plugin for your current IDE.
1. Install docker desktop.
1. In the docker-desktop settings, enable kubernetes. (This might take a few minutes.)

### Running the tests
Most IDEs will let you run individual tests or test fixtures straight from the code file. If you want to run all the tests, you can use `dotnet test`. You can control which tests to run by specifying which folder of tests to run. `dotnet test Tests/CodexTests` will run only the tests in `/Tests/CodexTests` and exclude the long tests.
