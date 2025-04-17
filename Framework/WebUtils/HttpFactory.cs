using Logging;

namespace WebUtils
{
    public interface IHttpFactory
    {
        IHttp CreateHttp(string id, Action<HttpClient> onClientCreated);
        IHttp CreateHttp(string id, Action<HttpClient> onClientCreated, IWebCallTimeSet timeSet);
        IHttp CreateHttp(string id);
    }

    public class HttpFactory : IHttpFactory
    {
        private readonly ILog log;
        private readonly IWebCallTimeSet defaultTimeSet;
        private readonly Action<HttpClient> factoryOnClientCreated;

        public HttpFactory(ILog log)
            : this (log, new DefaultWebCallTimeSet())
        {
        }

        public HttpFactory(ILog log, Action<HttpClient> onClientCreated)
            : this(log, new DefaultWebCallTimeSet(), onClientCreated)
        {
        }

        public HttpFactory(ILog log, IWebCallTimeSet defaultTimeSet)
            : this(log, defaultTimeSet, DoNothing)
        {
        }

        public HttpFactory(ILog log, IWebCallTimeSet defaultTimeSet, Action<HttpClient> onClientCreated)
        {
            this.log = log;
            this.defaultTimeSet = defaultTimeSet;
            this.factoryOnClientCreated = onClientCreated;
        }

        public IHttp CreateHttp(string id, Action<HttpClient> onClientCreated)
        {
            return CreateHttp(id, onClientCreated, defaultTimeSet);
        }

        public IHttp CreateHttp(string id, Action<HttpClient> onClientCreated, IWebCallTimeSet ts)
        {
            return new Http(id, log, ts, (c) =>
            {
                factoryOnClientCreated(c);
                onClientCreated(c);
            });
        }

        public IHttp CreateHttp(string id)
        {
            return new Http(id, log, defaultTimeSet, factoryOnClientCreated);
        }

        private static void DoNothing(HttpClient client)
        {
        }
    }
}
