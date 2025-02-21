using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3.Accounts;
using Utils;

namespace GethPlugin
{
    public static class EthAccountGenerator
    {
        public static EthAccount GenerateNew()
        {
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var account = new Account(privateKey);
            var ethAddress = new EthAddress(account.Address);

            return new EthAccount(ethAddress, account.PrivateKey);
        }
    }
}
