# Codex auto-client

This thing will generate files, upload them, and purchase storage for them in an endless loop.

Can generate random images or random data of a specified size.

## How to run

- dotnet 7.0 and CLI arguments: `dotnet run -- --codex-host=... --codex-port=...`
- docker and env-vars: `codexstorage/codex-autoclient:sha-88daab3`

## Configuration options
Options can be configured via CLI option or environment variable.

| CLI option              | Environment variable | Description                                                                                                         |
|-------------------------|----------------------|---------------------------------------------------------------------------------------------------------------------|
| "--codex-host"          | "CODEXHOST"          | Codex Host address. (default 'http://localhost')                                                                    |
| "--codex-port"          | "CODEXPORT"          | port number of Codex API. (8080 by default)                                                                         |
| "--datapath"            | "DATAPATH"           | Root path where all data files will be saved.                                                                       |
| "--purchases"           | "PURCHASES"          | Number of concurrent purchases.                                                                                     |
| "--contract-duration"   | "CONTRACTDURATION"   | contract duration in minutes. (default 6 hours)                                                                     |
| "--contract-expiry"     | "CONTRACTEXPIRY"     | contract expiry in minutes. (default 15 minutes)                                                                    |
| "--num-hosts"           | "NUMHOSTS"           | Number of hosts for contract. (default 5)                                                                           |
| "--num-hosts-tolerance" | "NUMTOL"             | Number of host tolerance for contract. (default 2)                                                                  |
| "--price"               | "PRICE"              | Price of contract. (default 10)                                                                                     |
| "--collateral"          | "COLLATERAL"         | Required collateral. (default 1)                                                                                    |
| "--filesizemb"          | "FILESIZEMB"         | When greater than zero, size of file generated and uploaded. When zero, random images are used instead. (default 0) |

## Timing

Configuration: `purchases` controls the number of concurrently running storage requests.
Configuration: `contract-duration` controls the duration in minutes of each storage request.
Auto-client will create a new storage request every X minutes, where X is the contract duration divided by the number of purchases.
(Timing may start to vary when contracts fail or time out.)
