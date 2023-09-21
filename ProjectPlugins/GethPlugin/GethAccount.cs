namespace GethPlugin
{
    public class GethAccount
    {
        public GethAccount(string account, string privateKey)
        {
            Account = account;
            PrivateKey = privateKey;
        }

        public string Account { get; }
        public string PrivateKey { get; }
    }
    
    public class AllGethAccounts
    {
        public GethAccount[] Accounts { get; }

        public AllGethAccounts(GethAccount[] accounts)
        {
            Accounts = accounts;
        }
    }
}
