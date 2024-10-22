using CodexOpenApi;
using IdentityModel.Client;
using Utils;

namespace BiblioTech
{
    public class CodexCidChecker
    {
        private static readonly string nl = Environment.NewLine;
        private readonly Configuration config;
        private CodexApi? currentCodexNode;

        public CodexCidChecker(Configuration config)
        {
            this.config = config;
        }

        public async Task<CheckResponse> PerformCheck(string cid)
        {
            if (string.IsNullOrEmpty(config.CodexEndpoint))
            {
                return new CheckResponse(false, "Codex CID checker is not (yet) available.", "");
            }

            try
            {
                var codex = GetCodex();
                var nodeCheck = await CheckCodex(codex);
                if (!nodeCheck) return new CheckResponse(false, "Codex node is not available. Cannot perform check.", $"Codex node at '{config.CodexEndpoint}' did not respond correctly to debug/info.");

                return await PerformCheck(codex, cid);
            }
            catch (Exception ex)
            {
                return new CheckResponse(false, "Internal server error", ex.ToString());
            }
        }

        private async Task<CheckResponse> PerformCheck(CodexApi codex, string cid)
        {
            try
            {
                var manifest = await codex.DownloadNetworkManifestAsync(cid);
                return SuccessMessage(manifest);
            }
            catch (ApiException apiEx)
            {
                if (apiEx.StatusCode == 400) return CidFormatInvalid(apiEx.Response);
                if (apiEx.StatusCode == 404) return FailedToFetch(apiEx.Response);
                return UnexpectedReturnCode(apiEx.Response);
            }
            catch (Exception ex)
            {
                return UnexpectedException(ex);
            }
        }

        #region Response formatting

        private CheckResponse SuccessMessage(DataItem content)
        {
            return FormatResponse(
                success: true,
                title: $"Success: '{content.Cid}'",
                error: "",
                $"size: {content.Manifest.OriginalBytes} bytes",
                $"blockSize: {content.Manifest.BlockSize} bytes",
                $"protected: {content.Manifest.Protected}"
            );
        }

        private CheckResponse UnexpectedException(Exception ex)
        {
            return FormatResponse(
                success: false,
                title: "Unexpected error",
                error: ex.ToString(),
                content: "Details will be sent to the bot-admin channel."
            );
        }

        private CheckResponse UnexpectedReturnCode(string response)
        {
            var msg = "Unexpected return code. Response: " + response;
            return FormatResponse(
                success: false,
                title: "Unexpected return code",
                error: msg,
                content: msg
            );
        }

        private CheckResponse FailedToFetch(string response)
        {
            var msg = "Failed to download content. Response: " + response;
            return FormatResponse(
                success: false,
                title: "Could not download content",
                error: msg,
                msg,
                $"Connection trouble? See 'https://docs.codex.storage/learn/troubleshoot'"
            );
        }

        private CheckResponse CidFormatInvalid(string response)
        {
            return FormatResponse(
                success: false,
                title: "Invalid format",
                error: "",
                content: "Provided CID is not formatted correctly."
            );
        }

        private CheckResponse FormatResponse(bool success, string title, string error, params string[] content)
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

            return new CheckResponse(success, msg, error);
        }

        #endregion

        #region Codex Node API 

        private CodexApi GetCodex()
        {
            if (currentCodexNode == null) currentCodexNode = CreateCodex();
            return currentCodexNode;
        }

        private async Task<bool> CheckCodex(CodexApi codex)
        {
            try
            {
                var info = await currentCodexNode!.GetDebugInfoAsync();
                if (info == null || string.IsNullOrEmpty(info.Id)) return false;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private CodexApi CreateCodex()
        {
            var endpoint = config.CodexEndpoint;
            var splitIndex = endpoint.LastIndexOf(':');
            var host = endpoint.Substring(0, splitIndex);
            var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

            var address = new Address(
                host: host,
                port: port
            );

            var client = new HttpClient();
            if (!string.IsNullOrEmpty(config.CodexEndpointAuth) && config.CodexEndpointAuth.Contains(":"))
            {
                var tokens = config.CodexEndpointAuth.Split(':');
                if (tokens.Length != 2) throw new Exception("Expected '<username>:<password>' in CodexEndpointAuth parameter.");
                client.SetBasicAuthentication(tokens[0], tokens[1]);    
            }

            var codex = new CodexApi(client);
            codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";
            return codex;
        }

        #endregion
    }

    public class CheckResponse
    {
        public CheckResponse(bool success, string message, string error)
        {
            Success = success;
            Message = message;
            Error = error;
        }

        public bool Success { get; }
        public string Message { get; }
        public string Error { get; }
    }
}
