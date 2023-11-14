# Codex Continuous Test-net Report
Date: 13-11-2023

Report for: 10-2023


## Test-net Status
- Start of month: Offline - stopped
- End of month: Offline - stopped

(Stopped: The number of tests that can successfully run on the test-net is not high enough to justify the cost of leaving it running.)

## Deployment Configuration
Continous Test-net is deployed to the kubernetes cluster with the following configuration:

5x Codex Nodes:
- Log-level: Trace
- Storage quota: 2048 MB
- Storage sell: 1024 MB
- Min price: 1024
- Max collateral: 1024
- Max duration: 3600000 seconds
- Block-TTL*: 180 seconds
- Block-MI*: 120 seconds
- Block-MN*: 10000 blocks

3 of these 5 nodes have:
- Validator: true

Kubernetes namespace: 'codex-continuous-tests'
* Some tests have been performed with alternative (disabled) maintenance parameters:
- Block-TTL: 99999999 seconds
- Block-MI: 99999999 seconds
- Block-MN: 100 blocks

## Test Overview
| Changes             | Test             | Description                    | Status     | Results                                                       |
|---------------------|------------------|--------------------------------|------------|---------------------------------------------------------------|
| No change           | Two-client test  | See report for July 2023.      | Faulted    | Test reliably fails. Both upload and download failures occur. |
| No change           | Two-client test* | See report for September 2023. | Faulted    | Test reliably fails. Both upload and download failures occur. |
| Possible regression | HoldMyBeer test  | See report for August 2023.    | Unreliable | Successful runs of 48h have not been observed in October.     |
| Possible regression | Peers test       | See report for August 2023.    | Unreliable | Successful runs of 48h have not been observed in October.     |

## Resulting changes
As a result of the testing efforts in 10-2023, these changes were made:
1. Consolidation of test logs and metrics using grafana and elastic-search.
1. Investment made in profiling instrumentation in Codex codebase.
1. Some testing effort has been diverted to preparing the necessary infrastructure for the creation of a public testnet by 1-December-2023.

## Action Points
- Debugging efforts continuou
- Some effort remains allocated to deploying and supporting the public testnet
