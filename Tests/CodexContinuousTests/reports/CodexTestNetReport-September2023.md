# Codex Continuous Test-net Report
Date: 05-10-2023

Report for: 09-2023


## Test-net Status
- Start of month: Offline - stopped
- End of month: Offline - stopped

(Stopped: The number of tests that can successfully run on the test-net is not high enough to justify the cost of leaving it running.)

## Deployment Configuration
Continuous Test-net is deployed to the kubernetes cluster with the following configuration:

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
| Changes        | Test             | Description                                                                        | Status  | Results                                                                                     |
|----------------|------------------|------------------------------------------------------------------------------------|---------|---------------------------------------------------------------------------------------------|
| No change      | Two-client test  | See report for July 2023.                                                          | Faulted | Test reliably fails. Both upload and download failures occur.                               |
| New in 09-2023 | Two-client test* | Modified Two-client test: Using disabled maintenance parameters and 8MB file size. | Faulted | Test reliably fails. Both upload and download failures occur.                               |
| No change      | HoldMyBeer test  | See report for August 2023.                                                        | Passed  | Successful run. No regression. Record run: 48h. After that the test was manually stopped.   |
| No change      | Peers test       | See report for August 2023.                                                        | Passed  | Successful run. No regression. Record run: 2d21h. After that the test was manually stopped. |

## Resulting changes
As a result of the testing efforts in 09-2023, these changes were made:
1. Cloud logging infrastructure was upgraded to reliably and verifiably retrieve Codex container logs.
1. Test runners and tools were upgraded to support automated deploy and run from CI environment.
1. Test runners and tools were upgraded to support to automatic building and use of custom locally-built docker images of Codex. (This allows to use of local modifications for quick debugging without the need to go through commit/review/CI workflow.)

## Action Points
- We'll be using the upgrades to debug the Two-client test.
