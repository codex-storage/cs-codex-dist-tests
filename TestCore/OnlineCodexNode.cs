using Newtonsoft.Json;
using NUnit.Framework;
using System.Net.Http.Headers;

namespace CodexDistTests.TestCore
{
    public interface IOnlineCodexNode
    {
        CodexDebugResponse GetDebugInfo();
        ContentId UploadFile(TestFile file, int retryCounter = 0);
        TestFile? DownloadContent(ContentId contentId);
    }

    public class OnlineCodexNode : IOnlineCodexNode
    {
        private readonly IFileManager fileManager;
        private readonly int port;

        public OfflineCodexNode Origin { get; }

        public OnlineCodexNode(OfflineCodexNode origin, IFileManager fileManager, int port)
        {
            Origin = origin;
            this.fileManager = fileManager;
            this.port = port;
        }

        public CodexDebugResponse GetDebugInfo()
        {
            return HttpGet<CodexDebugResponse>("debug/info");
        }

        public ContentId UploadFile(TestFile file, int retryCounter = 0)
        {
            try
            {
                var url = $"http://127.0.0.1:{port}/api/codex/v1/upload";
                using var client = GetClient();

                // Todo: If the file is too large to read into memory, we'll need to rewrite this upload POST to be streaming.
                var byteData = File.ReadAllBytes(file.Filename);
                using var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = Utils.Wait(client.PostAsync(url, content));

                var contentId = Utils.Wait(response.Content.ReadAsStringAsync());
                return new ContentId(contentId);
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
                    return UploadFile(file, retryCounter + 1);
                }
            }
        }

        public TestFile? DownloadContent(ContentId contentId)
        {
            // Todo: If the file is too large, rewrite to streaming:
            var bytes = HttpGetBytes("download/" + contentId.Id);
            if (bytes == null) return null;

            var file = fileManager.CreateEmptyTestFile();
            File.WriteAllBytes(file.Filename, bytes);
            return file;
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

    public class ContentId
    {
        public ContentId(string id)
        {
            Id = id;
        }
        
        public string Id { get; }
    }
}
