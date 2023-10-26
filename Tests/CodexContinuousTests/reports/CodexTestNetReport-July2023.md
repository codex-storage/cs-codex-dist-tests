# Codex Continuous Test-net Report
Date: 02-08-2023

Report for: 07-2023


## Test-net Status
- Start of month: Offline - faulted
- End of month: Offline - faulted

(Faulted: Tests fail with such frequency that the information gathered does not justify the cost of leaving the test-net running.)

## Deployment Configuration
Continuous Test-net is deployed to the kubernetes cluster with the following configuration:

5x Codex Nodes:
- Log-level: Trace
- Storage quota: 2048 MB
- Storage sell: 1024 MB
- Min price: 1024
- Max collateral: 1024
- Max duration: 3600000 seconds
- Block-TTL: 120 seconds

3 of these 5 nodes have:
- Validator: true

Kubernetes namespace: 'codex-continuous-tests'

## Test Overview
| Changes        | Test                | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    | Status      | Results                                                                              |
|----------------|---------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------|--------------------------------------------------------------------------------------|
| New in 07-2023 | Two-client test     | Every 30 seconds, two nodes are chosen at random. A 10 MB file is generated and uploaded to one. 10 seconds later, it is downloaded from the other. File contents are asserted to be equal.                                                                                                                                                                                                                                                                                                                                                                                    | Faulted     | Test reliably fails after 30 to 45 minutes. Both upload and download failures occur. |
| New in 07-2023 | Transient-node test | Every 1 minute, a new, transient Codex node is started and bootstrapped against a random node of the test net. A 10 MB file is generated and uploaded to the transient node. The file is then downloaded from a second random test net node and file equality is asserted. After that, the transient node is shut down. 30 seconds later, a new transient Codex node is started and bootstrapped against a third (guaranteed different from first and second) node from the test net. The same file is then downloaded from the new transient node. File equality is asserted. | Not running | Test was not run because the previous more rudamentory test has faulted.             |

## Action Points
- Codex logs (from these long-running containers) are often incomplete when downloaded after a test failure. A reliable way of maintaining these logs is needed. The logs can quickly explode in size, even for test-nets that run only for a few hours.
- The distributed testing setup can and has been used to reproduce the failure of the Two-client test in a local environment. Investigation is on-going.