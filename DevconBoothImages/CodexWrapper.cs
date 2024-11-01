using CodexOpenApi;
using System.Net.Http;
using System.Windows;
using Utils;

namespace DevconBoothImages
{
    public class CodexWrapper
    {
        public async Task<List<CodexApi>> GetCodexes()
        {
            var config = new Configuration();
            var result = new List<CodexApi>();

            foreach (var endpoint in config.CodexEndpoints)
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
            }

            return result;
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
