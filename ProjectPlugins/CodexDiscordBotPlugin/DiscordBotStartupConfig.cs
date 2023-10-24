using CodexPlugin;

namespace CodexDiscordBotPlugin
{
    public class DiscordBotStartupConfig
    {
        public DiscordBotStartupConfig(string name, string token, string serverName, string adminRoleName)
        {
            Name = name;
            Token = token;
            ServerName = serverName;
            AdminRoleName = adminRoleName;
        }

        public string Name { get; }
        public string Token { get; }
        public string ServerName { get; }
        public string AdminRoleName { get; }
    }
}
