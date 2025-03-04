using GethConnector;
using Logging;
using Utils;

namespace AutoClient.Modes.FolderStore
{
    public class BalanceChecker
    {
        private readonly LogPrefixer log;
        private readonly GethConnector.GethConnector? connector;
        private readonly EthAddress? address;

        public BalanceChecker(App app)
        {
            log = new LogPrefixer(app.Log, "(Balance) ");

            connector = GethConnector.GethConnector.Initialize(app.Log);
            address = LoadAddress(app);
        }

        private EthAddress? LoadAddress(App app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.Config.EthAddressFile)) return null;
                if (!File.Exists(app.Config.EthAddressFile)) return null;

                return new EthAddress(
                    File.ReadAllText(app.Config.EthAddressFile)
                    .Trim()
                    .Replace("\n", "")
                    .Replace(Environment.NewLine, "")
                );
            }
            catch (Exception exc)
            {
                log.Error($"Failed to load eth address from file: {exc}");
                return null;
            }
        }

        public void Check()
        {
            if (connector == null)
            {
                Log("Connector not configured. Can't check balances.");
                return;
            }
            if (address == null)
            {
                Log("EthAddress not found. Can't check balances.");
                return;
            }

            try
            {
                PerformCheck();
            }
            catch (Exception exc)
            {
                Log($"Exception while checking balances: {exc}");
            }
        }

        private void PerformCheck()
        {
            var geth = connector!.GethNode;
            var contracts = connector!.CodexContracts;
            var addr = address!;

            var eth = geth.GetEthBalance(addr);
            var tst = contracts.GetTestTokenBalance(addr);

            Log($"Balances: [{eth}] - [{tst}]");

            if (eth.Eth < 1) TryAddEth(geth, addr);
            if (tst.Tst < 1) TryAddTst(contracts, addr);
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
