using Logging;
using Utils;

namespace Core
{
    public interface IHttp
    {
        T OnClient<T>(Func<HttpClient, T> action);
        T OnClient<T>(Func<HttpClient, T> action, string description);
        T OnClient<T>(Func<HttpClient, T> action, Retry retry);
        IEndpoint CreateEndpoint(Address address, string baseUrl, string? logAlias = null);
    }

    internal class Http : IHttp
    {
        private static readonly object httpLock = new object();
        private readonly ILog log;
        private readonly ITimeSet timeSet;
        private readonly Action<HttpClient> onClientCreated;

        internal Http(ILog log, ITimeSet timeSet)
            : this(log, timeSet, DoNothing)
        {
        }

        internal Http(ILog log, ITimeSet timeSet, Action<HttpClient> onClientCreated)
        {
            this.log = log;
            this.timeSet = timeSet;
            this.onClientCreated = onClientCreated;
        }

        public T OnClient<T>(Func<HttpClient, T> action)
        {
            return OnClient(action, GetDescription());
        }

        public T OnClient<T>(Func<HttpClient, T> action, string description)
        {
            var retry = new Retry(description, timeSet.HttpRetryTimeout(), timeSet.HttpCallRetryDelay(), f => { });
            return OnClient(action, retry);
        }

        public T OnClient<T>(Func<HttpClient, T> action, Retry retry)
        {
            var client = GetClient();

            return LockRetry(() =>
            {
                return action(client);
            }, retry);
        }

        public IEndpoint CreateEndpoint(Address address, string baseUrl, string? logAlias = null)
        {
            return new Endpoint(log, this, address, baseUrl, logAlias);
        }

        private string GetDescription()
        {
            return DebugStack.GetCallerName(skipFrames: 2);
        }

        private T LockRetry<T>(Func<T> operation, Retry retry)
        {
            lock (httpLock)
            {
                return retry.Run(operation);
            }
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
