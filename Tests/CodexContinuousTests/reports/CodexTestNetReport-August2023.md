# Codex Continuous Test-net Report
Date: 05-09-2023

Report for: 08-2023


## Test-net Status
- Start of month: Offline - faulted
- End of month: Offline - stopped

(Faulted: Tests fail with such frequency that the information gathered does not justify the cost of leaving the test-net running.)

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
- Block-TTL: 180 seconds
- Block-MI: 120 seconds
- Block-MN: 10000 blocks

3 of these 5 nodes have:
- Validator: true

Kubernetes namespace: 'codex-continuous-tests'

## Test Overview
| Changes        | Test                | Description                                                                                                                                                                                                                                                   | Status      | Results                                                                               |
|----------------|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------|---------------------------------------------------------------------------------------|
| No change      | Two-client test     | See report for July 2023.                                                                                                                                                                                                                                     | Faulted     | Test reliably fails. Both upload and download failures occur.                         |
| No change      | Transient-node test | See report for July 2023.                                                                                                                                                                                                                                     | Not running | Test was not run because the previous more rudamentory test has faulted.              |
| New in 08-2023 | HoldMyBeer test     | Named so because the test is quite dumb and only holds a file, this test selects 1 node at random and uploads an 80 MB file. Then it downloads and verifies it. The purpose of this test is to isolate the upload/download behavior to identify issues in it. | Passed      | Several successful runs. Record run: 48h. After that the test was manually stopped.   |
| New in 08-2023 | Peers test          | Another attempt to isolate a portion of the two-client test behavior. This test periodically checks the connectivity status between each node in the network. It does this by analysing routing-table information and by performing a node-find operation.    | Passed      | Several successful runs. Record run: 2d21h. After that the test was manually stopped. |

## Resulting changes
As a result of the testing efforts in 08-2023, these three changes were made:
1. Block announcement performance - Block iteration (as part of the advertising loop) was found to be performing too much disc IO. Reducing this greatly increased client stability.
1. Component initialization order - A timing issue in the initialization order of the several Codex components caused nodes to improperly announce their addresses over the network in case the marketplace support was enabled.
1. Block maintenance performance - Block iteration (again but this time in maintenance) was causing the node to not respond to Ping messages quickly enough, causing other nodes to kick it from the network. By slowing down block maintenance this is solved. A future update of the DHT logic will also impact this behavior.

## Action Points
- Despite both HoldMyBeer and Peers test success, the two-client test is still not passing. In follow-up actions, we can investigate this issue further by decoupling block retrieval from file download.
