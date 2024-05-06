This code was generated using the Nethereum code generator, here: http://playground.nethereum.com

1. Go to site -> Abi Code Gen.
1. Contract name = "Marketplace".
1. In container, get "/hardhat/artifacts/contracts/Marketplace.sol/Marketplace.json".
1. Save only ABI section as new JSON. (top-level is a json array.)
1. From original JSON get byte code.
1. Put ABI JSON and byte code into site.
1. Generate.
1. From site generated code, copy `public partial class MarketplaceDeployment` and everything after it. (be considerate of namespace brackets!)
1. In Marketplace/Marketplace.cs, replace content of 'namespace CodexContractsPlugin.Marketplace'.



