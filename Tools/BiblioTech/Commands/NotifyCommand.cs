using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class NotifyCommand : BaseCommand
    {
        private readonly BoolOption boolOption = new BoolOption(name: "enabled", description: "Controls whether the bot will @-mention you.", isRequired: false);
        
        public override string Name => "notify";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Enable or disable notifications from the bot.";
        public override CommandOption[] Options => new CommandOption[] { boolOption };

        protected override async Task Invoke(CommandContext context)
        {
            var user = context.Command.User;
            var enabled = await boolOption.Parse(context);
            if (enabled == null) return;

            Program.UserRepo.SetUserNotificationPreference(user, enabled.Value);
            await context.Followup("Done!");
        }
    }
}
