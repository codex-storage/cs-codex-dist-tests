# Distributed System Tests for Nim-Codex

## Contributing tests
Do you want to write some tests for Codex using this distributed test setup? Great! Here's what you do.

1. Create a branch. Name it something descriptive, but start it with `tests/` please. [Example: `tests/data-redundancy`.]
1. Checkout your branch, and decide if your tests will be 'short' tests (minutes to hours), or 'long' tests (hours to days), or both! Create a folder for your tests in the matching folders (`/Tests`, `/LongTests`) and don't worry: You can always move your tests later if you like. [Example, short: `/Tests/DataRedundancy/`, long: `/LongTests/DataRedundancy/`]
1. Create one or more code files in your new folder, and write some tests! Here are some tips to help you get started. You can always take a look at the example tests found in [`/Tests/BasicTests/ExampleTests.cs`](/Tests/BasicTests/ExampleTests.cs)
   1. Set up a standard NUnit test fixture.
   1. Inherrit from `DistTest` or `AutoBootstrapDistTest`.
   1. When using `DistTest`:
      1. You must start your own Codex bootstrap node. You can use `SetupCodexBootstrapNode(...)` for this.
      1. When you start other Codex nodes with `SetupCodexNodes(...)` you can pass the bootstrap node by adding the `.WithBootstrapNode(...)` option.
   1. When using `AutoBootstrapDistTest`:
      1. The test-infra creates the bootstrap node for you, and automatically passes it to each Codex node you create in your tests. Handy for keeping your tests clean and to-the-point.
      1. When using the auto-bootstrap, you have no control over the bootstrap node from your tests. You can't (for example) shut it down during the course of the test. If you need this level of control for your scenario, use the `DistTest` instead.
   1. You can generate files of random test data by calling `GenerateTestFile(...)`.
   1. If your test needs a long time to run, add the `[UseLongTimeouts]` function attribute. This will greatly increase maximum time-out values for operations like for example uploading and downloading files.
   1. You can enable access to the Codex node metrics by adding the option `.EnableMetrics()`. Enabling metrics will make the test-infra download and save all Codex metrics in case of a test failure. (The metrics are stored as CSV, in the same location as the test log file.)
   1. You can enable access to the blockchain marketplace by adding the option `.EnableMarketplace(...)`.
   1. Enabling metrics and/or enabling the marketplace takes extra resources from the test-infra and increases the time needed during Codex node setup. Please don't enable these features unless your tests need them.
   1. Tip: Codex nodes can be named. Use the option `WithName(...)` and make reading your test logs a little nicer!
   1. Tip: Commit often.
1. Once you're happy with your tests, please create a pull-request and ask (another) Codex core contributor to review your changes.
