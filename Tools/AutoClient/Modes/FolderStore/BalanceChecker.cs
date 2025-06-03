using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class BalanceChecker
    {
        private readonly LogPrefixer log;
        private readonly GethConnector.GethConnector? connector;
        private readonly EthAddress[] addresses;

        public BalanceChecker(App app)
        {
            log = new LogPrefixer(app.Log, "(Balance) ");

            connector = GethConnector.GethConnector.Initialize(app.Log);
            addresses = LoadAddresses(app);

            log.Log($"Loaded Eth-addresses for checking: {addresses.Length}");
            foreach (var addr in addresses) log.Log(" - " + addr);
        }

        private EthAddress[] LoadAddresses(App app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.Config.EthAddressFile)) return Array.Empty<EthAddress>();

                var tokens = app.Config.EthAddressFile.Split(";", StringSplitOptions.RemoveEmptyEntries);
                return tokens.Select(ConvertToAddress).Where(a => a != null).Cast<EthAddress>().ToArray();
            }
            catch (Exception exc)
            {
                log.Error($"Failed to load eth address from file: {exc}");
                return Array.Empty<EthAddress>();
            }
        }

        private EthAddress? ConvertToAddress(string t)
        {
            if (!File.Exists(t)) return null;
            return new EthAddress(
                    File.ReadAllText(t)
                    .Trim()
                    .Replace("\n", "")
                    .Replace(Environment.NewLine, ""));
        }

        public void Check()
        {
            if (connector == null)
            {
                Log("Connector not configured. Can't check balances.");
                return;
            }

            Log("Checking balances...");
            foreach (var address in addresses)
            {
                try
                {
                    PerformCheck(address);
                }
                catch (Exception exc)
                {
                    Log($"Exception while checking balances: {exc}");
                }
            }
        }

        private void PerformCheck(EthAddress address)
        {
            var geth = connector!.GethNode;
            var contracts = connector!.CodexContracts;

            var eth = geth.GetEthBalance(address);
            var tst = contracts.GetTestTokenBalance(address);

            Log($"Balances: [{eth}] - [{tst}]");

            if (eth.Eth < 1) TryAddEth(geth, address);
            if (tst.Tst < 1) TryAddTst(contracts, address);
        }

        private void TryAddEth(GethPlugin.IGethNode geth, EthAddress addr)
        {
            try
            {
                var amount = 100.Eth();
                var result = geth.SendEth(addr, amount);
                Log($"Successfull added {amount} - {result}");
            }
            catch (Exception exc)
            {
                Log("Failed to add eth: " + exc);
            }
        }

        private void TryAddTst(CodexContractsPlugin.ICodexContracts contracts, EthAddress addr)
        {
            try
            {
                var amount = 100.Tst();
                var result = contracts.MintTestTokens(addr, amount);
                Log($"Successfull added {amount} - {result}");
            }
            catch (Exception exc)
            {
                Log("Failed to add testtokens: " + exc);
            }
        }

        private void Log(string msg)
        {
            log.Log(msg);
        }
    }
}
