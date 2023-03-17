using Newtonsoft.Json;
using NUnit.Framework;
using System.Net.Http.Headers;

namespace CodexDistTests.TestCore
{
    public class CodexNode
    {
        private readonly int port;

        public CodexNode(int port)
        {
            this.port = port;
        }

        public CodexDebugResponse GetDebugInfo()
        {
            return HttpGet<CodexDebugResponse>("debug/info");
        }

        public string UploadFile(string filename, int retryCounter = 0)
        {
            try
            {
                var url = $"http://127.0.0.1:{port}/api/codex/v1/upload";
                using var client = GetClient();

                var byteData = File.ReadAllBytes(filename);
                using var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = Utils.Wait(client.PostAsync(url, content));

                var contentId = Utils.Wait(response.Content.ReadAsStringAsync());
                return contentId;
            }
            catch (Exception exception)
            {
                if (retryCounter > 5)
                {
                    Assert.Fail(exception.Message);
                    throw;
                }
                else
                {
                    Timing.RetryDelay();
                    return UploadFile(filename, retryCounter + 1);
                }
            }
        }

        public byte[]? DownloadContent(string contentId)
        {
            return HttpGetBytes("download/" + contentId);
        }

        private byte[]? HttpGetBytes(string endpoint, int retryCounter = 0)
        {
            try
            {
                using var client = GetClient();
                var url = $"http://127.0.0.1:{port}/api/codex/v1/" + endpoint;
                var result = Utils.Wait(client.GetAsync(url));
                return Utils.Wait(result.Content.ReadAsByteArrayAsync());
            }
            catch (Exception exception)
            {
                if (retryCounter > 5)
                {
                    Assert.Fail(exception.Message);
                    return null;
                }
                else
                {
                    Timing.RetryDelay();
                    return HttpGetBytes(endpoint, retryCounter + 1);
                }
            }
        }

        private T HttpGet<T>(string endpoint, int retryCounter = 0)
        {
            try
            {
                using var client = GetClient();
                var url = $"http://127.0.0.1:{port}/api/codex/v1/" + endpoint;
                var result = Utils.Wait(client.GetAsync(url));
                var json = Utils.Wait(result.Content.ReadAsStringAsync());
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception exception)
            {
                if (retryCounter > 5)
                {
                    Assert.Fail(exception.Message);
                    throw;
                }
                else
                {
                    Timing.RetryDelay();
                    return HttpGet<T>(endpoint, retryCounter + 1);
                }
            }
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient();
            client.Timeout = Timing.HttpCallTimeout();
            return client;
        }
    }

    public class CodexDebugResponse
    {
        public string id { get; set; } = string.Empty;
        public string[] addrs { get; set; } = new string[0];
        public string repo { get; set; } = string.Empty;
        public string spr { get; set; } = string.Empty;
        public CodexDebugVersionResponse codex { get; set; } = new();
    }

    public class CodexDebugVersionResponse
    {
        public string version { get; set; } = string.Empty;
        public string revision { get; set; } = string.Empty;
    }
}
