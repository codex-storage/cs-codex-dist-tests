using CodexClient;
using IdentityModel.Client;
using Logging;
using Utils;
using WebUtils;

namespace BiblioTech.CodexChecking
{
    public class CodexWrapper
    {
        private readonly CodexNodeFactory factory;
        private readonly ILog log;
        private readonly Configuration config;
        private readonly object codexLock = new object();
        private ICodexNode? currentCodexNode;

        public CodexWrapper(ILog log, Configuration config)
        {
            this.log = log;
            this.config = config;

            var httpFactory = CreateHttpFactory();
            factory = new CodexNodeFactory(log, httpFactory, dataDir: config.DataPath);

            Task.Run(CheckCodexNode);
        }

        public T? OnCodex<T>(Func<ICodexNode, T> func) where T : class
        {
            lock (codexLock)
            {
                if (currentCodexNode == null) return null;
                return func(currentCodexNode);
            }
        }

        private void CheckCodexNode()
        {
            Thread.Sleep(TimeSpan.FromSeconds(10.0));

            while (true)
            {
                lock (codexLock)
                {
                    var newNode = GetNewCodexNode();
                    if (newNode != null && currentCodexNode == null) ShowConnectionRestored();
                    if (newNode == null && currentCodexNode != null) ShowConnectionLost();
                    currentCodexNode = newNode;
                }

                Thread.Sleep(TimeSpan.FromMinutes(15.0));
            }
        }

        private ICodexNode? GetNewCodexNode()
        {
            try
            {
                if (currentCodexNode != null)
                {
                    try
                    {
                        // Current instance is responsive? Keep it.
                        var info = currentCodexNode.GetDebugInfo();
                        if (info != null && info.Version != null &&
                            !string.IsNullOrEmpty(info.Version.Revision)) return currentCodexNode;
                    }
                    catch
                    {
                    }
                }

                return CreateCodex();
            }
            catch (Exception ex)
            {
                log.Error("Exception when trying to check codex node: " + ex.Message);
                return null;
            }
        }

        private void ShowConnectionLost()
        {
            Program.AdminChecker.SendInAdminChannel("Codex node connection lost.");
        }

        private void ShowConnectionRestored()
        {
            Program.AdminChecker.SendInAdminChannel("Codex node connection restored.");
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

        private HttpFactory CreateHttpFactory()
        {
            if (string.IsNullOrEmpty(config.CodexEndpointAuth) || !config.CodexEndpointAuth.Contains(":"))
            {
                return new HttpFactory(log, new SnappyTimeSet());
            }

            var tokens = config.CodexEndpointAuth.Split(':');
            if (tokens.Length != 2) throw new Exception("Expected '<username>:<password>' in CodexEndpointAuth parameter.");

            return new HttpFactory(log, new SnappyTimeSet(), onClientCreated: client =>
            {
                client.SetBasicAuthentication(tokens[0], tokens[1]);
            });
        }

        public class SnappyTimeSet : IWebCallTimeSet
        {
            public TimeSpan HttpCallRetryDelay()
            {
                return TimeSpan.FromSeconds(1.0);
            }

            public TimeSpan HttpCallTimeout()
            {
                return TimeSpan.FromSeconds(3.0);
            }

            public TimeSpan HttpRetryTimeout()
            {
                return TimeSpan.FromSeconds(12.0);
            }
        }
    }
}
