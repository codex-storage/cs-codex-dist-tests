using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3.Accounts;

namespace CodexPlugin
{
    public class MarketplaceStarter
    {
        public MarketplaceStartResults Start()
        {
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var account = new Account(privateKey);

            return new MarketplaceStartResults(account.Address, account.PrivateKey);
        }
    }
}
