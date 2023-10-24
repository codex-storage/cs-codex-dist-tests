using CodexPlugin;

namespace CodexDiscordBotPlugin
{
    public class DiscordBotStartupConfig
    {
        public DiscordBotStartupConfig(string name, string token, string serverName, string adminRoleName, CodexDeployment codexDeployment)
        {
            Name = name;
            Token = token;
            ServerName = serverName;
            AdminRoleName = adminRoleName;
            CodexDeployment = codexDeployment;
        }

        public string Name { get; }
        public string Token { get; }
        public string ServerName { get; }
        public string AdminRoleName { get; }
        public CodexDeployment CodexDeployment { get; }
    }
}
