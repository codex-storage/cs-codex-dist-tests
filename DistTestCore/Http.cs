using Newtonsoft.Json;
using NUnit.Framework;
using System.Net.Http.Headers;

namespace DistTestCore
{
    public class Http
    {
        private readonly string ip;
        private readonly int port;
        private readonly string baseUrl;

        public Http(string ip, int port, string baseUrl)
        {
            this.ip = ip;
            this.port = port;
            this.baseUrl = baseUrl;

            if (!this.baseUrl.StartsWith("/")) this.baseUrl = "/" + this.baseUrl;
            if (!this.baseUrl.EndsWith("/")) this.baseUrl += "/";
        }

        public string HttpGetString(string route)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;
                var result = Utils.Wait(client.GetAsync(url));
                return Utils.Wait(result.Content.ReadAsStringAsync());
            });
        }

        public T HttpGetJson<T>(string route)
        {
            return JsonConvert.DeserializeObject<T>(HttpGetString(route))!;
        }

        public string HttpPostStream(string route, Stream stream)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;

                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = Utils.Wait(client.PostAsync(url, content));

                return Utils.Wait(response.Content.ReadAsStringAsync());
            });
        }

        public Stream HttpGetStream(string route)
        {
            return Retry(() =>
            {
                var client = GetClient();
                var url = GetUrl() + route;

                return Utils.Wait(client.GetStreamAsync(url));
            });
        }

        private string GetUrl()
        {
            return $"http://{ip}:{port}{baseUrl}";
        }

        private static T Retry<T>(Func<T> operation)
        {
            var retryCounter = 0;

            while (true)
            {
                try
                {
                    return operation();
                }
                catch (Exception exception)
                {
                    Timing.HttpCallRetryDelay();
                    retryCounter++;
                    if (retryCounter > Timing.HttpCallRetryCount())
                    {
                        Assert.Fail(exception.Message);
                        throw;
                    }
                }
            }
        }

        private static HttpClient GetClient()
        {
            var client = new HttpClient();
            client.Timeout = Timing.HttpCallTimeout();
            return client;
        }
    }
}
