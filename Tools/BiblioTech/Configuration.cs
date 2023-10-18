using ArgsUniform;

namespace BiblioTech
{
    public class Configuration
    {
        [Uniform("token", "t", "TOKEN", true, "Discord Application Token")]
        public string ApplicationToken { get; set; } = string.Empty;

        [Uniform("deploys", "d", "DEPLOYS", false, "Path where deployment JSONs are located.")]
        public string DeploymentsPath { get; set; } = "deploys";
    }
}
