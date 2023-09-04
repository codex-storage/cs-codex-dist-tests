using Logging;
using Newtonsoft.Json;
using Serialization = Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Utils;

namespace DistTestCore
{
    public class Http
    {
        private readonly BaseLog log;
        private readonly ITimeSet timeSet;
        private readonly Address address;
        private readonly string baseUrl;
        private readonly Action<HttpClient> onClientCreated;
        private readonly string? logAlias;

        public Http(BaseLog log, ITimeSet timeSet, Address address, string baseUrl, string? logAlias = null)
            : this(log, timeSet, address, baseUrl, DoNothing, logAlias)
        {
        }

        public Http(BaseLog log, ITimeSet timeSet, Address address, string baseUrl, Action<HttpClient> onClientCreated, string? logAlias = null)
        {
            this.log = log;
            this.timeSet = timeSet;
            this.address = address;
            this.baseUrl = baseUrl;
            this.onClientCreated = onClientCreated;
            this.logAlias = logAlias;
            if (!this.baseUrl.StartsWith("/")) this.baseUrl = "/" + this.baseUrl;
            if (!this.baseUrl.EndsWith("/")) this.baseUrl += "/";
        }

        public string HttpGetString(string route)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;
                Log(url, "");
                var result = Time.Wait(client.GetAsync(url));
                var str = Time.Wait(result.Content.ReadAsStringAsync());
                Log(url, str);
                return str; ;
            }, $"HTTP-GET:{route}");
        }

        public T HttpGetJson<T>(string route)
        {
            var json = HttpGetString(route);
            return TryJsonDeserialize<T>(json);
        }

        public TResponse HttpPostJson<TRequest, TResponse>(string route, TRequest body)
        {
            var response = HttpPostJson(route, body);
            var json = Time.Wait(response.Content.ReadAsStringAsync());
            if(!response.IsSuccessStatusCode) {
                throw new HttpRequestException(json);
            }
            Log(GetUrl() + route, json);
            return TryJsonDeserialize<TResponse>(json);
        }

        public HttpResponseMessage HttpPostJson<TRequest>(string route, TRequest body)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;
                using var content = JsonContent.Create(body);
                Log(url, JsonConvert.SerializeObject(body));
                return Time.Wait(client.PostAsync(url, content));
            }, $"HTTP-POST-JSON: {route}");
        }

        public string HttpPostString(string route, string body)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;
                Log(url, body);
                var content = new StringContent(body);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var result = Time.Wait(client.PostAsync(url, content));
                var str = Time.Wait(result.Content.ReadAsStringAsync());
                Log(url, str);
                return str;
            }, $"HTTP-POST-STRING: {route}");
        }

        public string HttpPostStream(string route, Stream stream)
        {
            return Retry(() =>
            {
                using var client = GetClient();
                var url = GetUrl() + route;
                Log(url, "~ STREAM ~");
                var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = Time.Wait(client.PostAsync(url, content));
                var str = Time.Wait(response.Content.ReadAsStringAsync());
                Log(url, str);
                return str;
            }, $"HTTP-POST-STREAM: {route}");
        }

        public Stream HttpGetStream(string route)
        {
            return Retry(() =>
            {
                var client = GetClient();
                var url = GetUrl() + route;
                Log(url, "~ STREAM ~");
                return Time.Wait(client.GetStreamAsync(url));
            }, $"HTTP-GET-STREAM: {route}");
        }

        public T TryJsonDeserialize<T>(string json)
        {
            var errors = new List<string>();
            var deserialized = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings(){
                Error = delegate(object? sender, Serialization.ErrorEventArgs args)
                {
                    if (args.CurrentObject == args.ErrorContext.OriginalObject)
                    {
                        errors.Add($"""
                                    Member: '{args.ErrorContext.Member?.ToString() ?? "<null>"}'
                                    Path: {args.ErrorContext.Path}
                                    Error: {args.ErrorContext.Error.Message}
                                    """);
                        args.ErrorContext.Handled = true;
                    }
                }
            });
            if (errors.Count() > 0) {
                throw new JsonSerializationException($"Failed to deserialize JSON '{json}' with exception(s): \n{string.Join("\n", errors)}");
            }
            else if (deserialized == null) {
                throw new JsonSerializationException($"Failed to deserialize JSON '{json}': resulting deserialized object is null");
            }
            return deserialized;
        }

        private string GetUrl()
        {
            return $"{address.Host}:{address.Port}{baseUrl}";
        }

        private void Log(string url, string message)
        {
            if (logAlias != null)
            {
                log.Debug($"({logAlias})({url}) = '{message}'", 3);
            }
            else
            {
                log.Debug($"({url}) = '{message}'", 3);
            }
        }

        private T Retry<T>(Func<T> operation, string description)
        {
            return Time.Retry(operation, timeSet.HttpCallRetryTime(), timeSet.HttpCallRetryDelay(), description);
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient();
            client.Timeout = timeSet.HttpCallTimeout();
            onClientCreated(client);
            return client;
        }

        private static void DoNothing(HttpClient client)
        {
        }
    }
}
