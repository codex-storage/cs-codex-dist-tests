using KubernetesWorkflow;
using Logging;
using Utils;

namespace GethPlugin
{
    public class GethContainerInfoExtractor
    {
        private readonly ILog log;
        private readonly IStartupWorkflow workflow;
        private readonly RunningContainer container;

        public GethContainerInfoExtractor(ILog log, IStartupWorkflow workflow, RunningContainer container)
        {
            this.log = log;
            this.workflow = workflow;
            this.container = container;
        }

        public AllGethAccounts ExtractAccounts()
        {
            log.Debug();
            var accountsCsv = Retry(() => FetchAccountsCsv());
            if (string.IsNullOrEmpty(accountsCsv)) throw new InvalidOperationException("Unable to fetch accounts.csv for geth node. Test infra failure.");

            var lines = accountsCsv.Split('\n');
            return new AllGethAccounts(lines.Select(ParseLineToAccount).ToArray());
        }

        public string ExtractPubKey()
        {
            log.Debug();
            var pubKey = Retry(FetchPubKey);
            if (string.IsNullOrEmpty(pubKey)) throw new InvalidOperationException("Unable to fetch enode from geth node. Test infra failure.");

            return pubKey;
        }

        private string FetchAccountsCsv()
        {
            return workflow.ExecuteCommand(container, "cat", GethContainerRecipe.AccountsFilename);
        }

        private string FetchPubKey()
        {
            var enodeFinder = new PubKeyFinder(s => log.Debug(s));
            workflow.DownloadContainerLog(container, enodeFinder, null);
            return enodeFinder.GetPubKey();
        }

        private GethAccount ParseLineToAccount(string l)
        {
            var tokens = l.Replace("\r", "").Split(',');
            if (tokens.Length != 2) throw new InvalidOperationException();
            var account = tokens[0];
            var privateKey = tokens[1];
            return new GethAccount(account, privateKey);
        }

        private static string Retry(Func<string> fetch)
        {
            return Time.Retry(fetch, nameof(GethContainerInfoExtractor));
        }
    }

    public class PubKeyFinder : LogHandler, ILogHandler
    {
        private const string openTag = "self=enode://";
        private const string openTagQuote = "self=\"enode://";
        private readonly Action<string> debug;
        private string pubKey = string.Empty;

        public PubKeyFinder(Action<string> debug)
        {
            this.debug = debug;
            debug($"Looking for '{openTag}' in container logs...");
        }

        public string GetPubKey()
        {
            if (string.IsNullOrEmpty(pubKey)) throw new Exception("Not found yet exception.");
            return pubKey;
        }

        protected override void ProcessLine(string line)
        {
            debug(line);
            if (line.Contains(openTag))
            {
                ExtractPubKey(openTag, line);
            }
            else if (line.Contains(openTagQuote))
            {
                ExtractPubKey(openTagQuote, line);
            }
        }

        private void ExtractPubKey(string tag, string line)
        {
            var openIndex = line.IndexOf(tag) + tag.Length;
            var closeIndex = line.IndexOf("@");

            pubKey = line.Substring(
                    startIndex: openIndex,
                    length: closeIndex - openIndex);
        }
    }
}
