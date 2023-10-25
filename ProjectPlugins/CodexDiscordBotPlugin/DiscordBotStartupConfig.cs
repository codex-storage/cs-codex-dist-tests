namespace CodexDiscordBotPlugin
{
    public class DiscordBotStartupConfig
    {
        public DiscordBotStartupConfig(string name, string token, string serverName, string adminRoleName, string adminChannelName)
        {
            Name = name;
            Token = token;
            ServerName = serverName;
            AdminRoleName = adminRoleName;
            AdminChannelName = adminChannelName;
        }

        public string Name { get; }
        public string Token { get; }
        public string ServerName { get; }
        public string AdminRoleName { get; }
        public string AdminChannelName { get; }
    }
}
