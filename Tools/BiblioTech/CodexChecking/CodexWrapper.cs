﻿using CodexClient;
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
        }

        public void OnCodex(Action<ICodexNode> action)
        {
            lock (codexLock)
            {
                action(Get());
            }
        }

        public T OnCodex<T>(Func<ICodexNode, T> func)
        {
            lock (codexLock)
            {
                return func(Get());
            }
        }

        private ICodexNode Get()
        {
            if (currentCodexNode == null)
            {
                currentCodexNode = CreateCodex();
            }

            return currentCodexNode;
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
                return new HttpFactory(log);
            }

            var tokens = config.CodexEndpointAuth.Split(':');
            if (tokens.Length != 2) throw new Exception("Expected '<username>:<password>' in CodexEndpointAuth parameter.");

            return new HttpFactory(log, onClientCreated: client =>
            {
                client.SetBasicAuthentication(tokens[0], tokens[1]);
            });
        }
    }
}
