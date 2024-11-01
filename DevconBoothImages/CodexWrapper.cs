using CodexOpenApi;
using IdentityModel.Client;
using System.Net.Http;
using System.Windows;
using Utils;

namespace DevconBoothImages
{
    public class Codexes
    {
        public Codexes(CodexApi local, CodexApi testnet)
        {
            Local = local;
            Testnet = testnet;
        }

        public CodexApi Local { get; }
        public CodexApi Testnet { get; }
    }

    public class CodexWrapper
    {
        public async Task<Codexes> GetCodexes()
        {
            var config = new Configuration();
            return new Codexes(
                await GetCodexWithPort(config.CodexLocalEndpoint),
                await GetCodexWithoutPort(config.CodexPublicEndpoint, config.AuthUser, config.AuthPw)
            );
        }

        private async Task<CodexApi> GetCodexWithPort(string endpoint)
        {
            var splitIndex = endpoint.LastIndexOf(':');
            var host = endpoint.Substring(0, splitIndex);
            var port = Convert.ToInt32(endpoint.Substring(splitIndex + 1));

            var address = new Address(
                host: host,
                port: port
            );

            var client = new HttpClient();
            var codex = new CodexApi(client);
            codex.BaseUrl = $"{address.Host}:{address.Port}/api/codex/v1";

            await CheckCodex(codex, endpoint);
            return codex;
        }

        private async Task<CodexApi> GetCodexWithoutPort(string endpoint, string user, string pw)
        {
            var client = new HttpClient();
            client.SetBasicAuthentication(user, pw);
            var codex = new CodexApi(client);
            codex.BaseUrl = $"{endpoint}/api/codex/v1";

            await CheckCodex(codex, endpoint);
            return codex;
        }

        private async Task CheckCodex(CodexApi codex, string endpoint)
        {
            try
            {
                var info = await codex.GetDebugInfoAsync();
                if (string.IsNullOrEmpty(info.Id)) throw new Exception("Failed to fetch Codex node id");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to codex '{endpoint}': {ex}");
                throw;
            }
        }

    }
}
