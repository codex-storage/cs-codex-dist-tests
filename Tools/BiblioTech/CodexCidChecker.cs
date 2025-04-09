using CodexClient;
using Logging;
using Utils;

namespace BiblioTech
{
    public class CodexCidChecker
    {
        private static readonly string nl = Environment.NewLine;
        private readonly Configuration config;
        private readonly ILog log;
        private readonly Mutex checkMutex = new Mutex();
        private readonly CodexNodeFactory factory;
        private ICodexNode? currentCodexNode;

        public CodexCidChecker(Configuration config, ILog log)
        {
            this.config = config;
            this.log = log;

            factory = new CodexNodeFactory(log, dataDir: config.DataPath);

            if (!string.IsNullOrEmpty(config.CodexEndpointAuth) && config.CodexEndpointAuth.Contains(":"))
            {
                throw new Exception("Todo: codexnodefactory httpfactory support basicauth!");
                //var tokens = config.CodexEndpointAuth.Split(':');
                //if (tokens.Length != 2) throw new Exception("Expected '<username>:<password>' in CodexEndpointAuth parameter.");
                //client.SetBasicAuthentication(tokens[0], tokens[1]);
            }
        }

        public CheckResponse PerformCheck(string cid)
        {
            if (string.IsNullOrEmpty(config.CodexEndpoint))
            {
                return new CheckResponse(false, "Codex CID checker is not (yet) available.");
            }

            try
            {
                checkMutex.WaitOne();
                var codex = GetCodex();
                var nodeCheck = CheckCodex(codex);
                if (!nodeCheck) return new CheckResponse(false, "Codex node is not available. Cannot perform check.");

                return PerformCheck(codex, cid);
            }
            finally
            {
                checkMutex.ReleaseMutex();
            }
        }

        private CheckResponse PerformCheck(ICodexNode codex, string cid)
        {
            try
            {
                var manifest = codex.DownloadManifestOnly(new ContentId(cid));
                return SuccessMessage(manifest);
            }
            catch
            {
                return FailedMessage();
            }
        }

        #region Response formatting

        private CheckResponse SuccessMessage(LocalDataset content)
        {
            return FormatResponse(
                success: true,
                title: $"Success",
                $"cid: '{content.Cid}'",
                $"size: {content.Manifest.OriginalBytes} bytes",
                $"blockSize: {content.Manifest.BlockSize} bytes",
                $"protected: {content.Manifest.Protected}"
            );
        }

        private CheckResponse FailedMessage()
        {
            var msg = "Could not download content.";

            return FormatResponse(
                success: false,
                title: "Failed",
                msg,
                $"Connection trouble? See 'https://docs.codex.storage/learn/troubleshoot'"
            );
        }

        private CheckResponse FormatResponse(bool success, string title, params string[] content)
        {
            var msg = string.Join(nl,
                new string[]
                {
                    title,
                    "```"
                }
                .Concat(content)
                .Concat(new string[]
                {
                    "```"
                })
            ) + nl + nl;

            return new CheckResponse(success, msg);
        }

        #endregion

        #region Codex Node API 

        private ICodexNode GetCodex()
        {
            if (currentCodexNode == null) currentCodexNode = CreateCodex();
            return currentCodexNode;
        }

        private bool CheckCodex(ICodexNode node)
        {
            try
            {
                var info = node.GetDebugInfo();
                if (info == null || string.IsNullOrEmpty(info.Id)) return false;
                return true;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return false;
            }
        }

        private ICodexNode CreateCodex()
        {
            var endpoint = config.CodexEndpoint;
            var splitIndex = endpoint.LastIndexOf(':');
            var host = endpoint.Substring(0, splitIndex);
            var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

            var address = new Address(
                logName: $"cdx@{host}:{port}",
                host: host,
                port: port
            );

            var instance = CodexInstance.CreateFromApiEndpoint("ac", address);
            return factory.CreateCodexNode(instance);
        }

        #endregion
    }

    public class CheckResponse
    {
        public CheckResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }
    }
}
