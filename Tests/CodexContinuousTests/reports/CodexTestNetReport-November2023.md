# Codex Continuous Test-net Report
Date: 05-12-2023

Report for: 11-2023


## Continuous test-net Status
Continuous test runs (which can take many hours or days) can easily be started by team member from the github actions UI. Results are collected and displayed in Grafana. For the time being, we're suspending the effort to have a network of Codex nodes "always online" and continuously being tested, until overal reliability improves.

## Deployment Configuration
Continous Test-net is deployed to the kubernetes cluster with the following configuration:

5x Codex Nodes:
- Log-level: Trace
- Storage quota: 20480 MB
- Storage sell: 1024 MB
- Min price: 1024
- Max collateral: 1024
- Max duration: 3600000 seconds
- Block-TTL*: 99999999 seconds
- Block-MI*: 99999999 seconds
- Block-MN*: 100 blocks
3 of these 5 nodes have:
- Validator: true

## Test Overview
| Changes | Test             | Description                    | Status  | Results              |
|---------|------------------|--------------------------------|---------|----------------------|
| todo    | Two-client test  | See report for July 2023.      | Faulted | Test reliably fails. |
| todo    | Two-client test* | See report for September 2023. | Faulted | Test reliably fails. |
| todo    | HoldMyBeer test  | See report for August 2023.    | todo    | todo                 |
| todo    | Peers test       | See report for August 2023.    | todo    | todo                 |

## Resulting changes
As a result of the testing efforts in 11-2023, these changes were made:
1. todo

## Action Points
- Debugging efforts continuou
- 

