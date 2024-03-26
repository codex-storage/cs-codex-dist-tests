using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3.Accounts;

namespace GethPlugin
{
    [Serializable]
    public class EthAccount
    {
        public EthAccount(EthAddress ethAddress, string privateKey)
        {
            EthAddress = ethAddress;
            PrivateKey = privateKey;
        }

        public EthAddress EthAddress { get; }
        public string PrivateKey { get; }

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
