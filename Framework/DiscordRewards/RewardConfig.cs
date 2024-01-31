namespace DiscordRewards
{
    public class RewardConfig
    {
        public const string UsernameTag = "<USER>";

        public RewardConfig(ulong roleId, string message, CheckConfig checkConfig)
        {
            RoleId = roleId;
            Message = message;
            CheckConfig = checkConfig;
        }

        public ulong RoleId { get; }
        public string Message { get; }
        public CheckConfig CheckConfig { get; }
    }
}
