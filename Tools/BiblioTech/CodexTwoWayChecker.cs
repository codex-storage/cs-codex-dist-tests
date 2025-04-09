using CodexClient;
using IdentityModel.Client;
using Logging;
using Utils;
using WebUtils;

namespace BiblioTech
{
    public class CodexTwoWayChecker
    {
        private static readonly string nl = Environment.NewLine;
        private readonly Configuration config;
        private readonly ILog log;
        private readonly CodexNodeFactory factory;
        private ICodexNode? currentCodexNode;

        public CodexTwoWayChecker(Configuration config, ILog log)
        {
            this.config = config;
            this.log = log;

            var httpFactory = CreateHttpFactory();

            factory = new CodexNodeFactory(log, httpFactory, dataDir: config.DataPath);
        }

        // down check:
        // generate unique data
        // upload to cloud node
        // give CID to user to download
        // user inputs unique data into command to clear this check

        // up check:
        // generate unique data
        // create file and send it to user via discord api
        // user uploads and gives CID via command
        // download manifest: file is not larger than expected
        // download file: contents is unique data -> clear this check

        // both checks: altruistic role

        private HttpFactory CreateHttpFactory()
        {
            if (string.IsNullOrEmpty(config.CodexEndpointAuth) && config.CodexEndpointAuth.Contains(":"))
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
