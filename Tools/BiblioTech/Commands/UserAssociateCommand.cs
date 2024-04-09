using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class UserAssociateCommand : BaseCommand
    {
        public UserAssociateCommand(NotifyCommand notifyCommand)
        {
            this.notifyCommand = notifyCommand;
        }
        
        private readonly NotifyCommand notifyCommand;
        private readonly EthAddressOption ethOption = new EthAddressOption(isRequired: false);
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, associates Ethereum address for another user. (Optional, admin-only)",
            isRequired: false);

        public override string Name => "set";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Associates a Discord user with an Ethereum address.";
        public override CommandOption[] Options => new CommandOption[] { ethOption, optionalUser };

        protected override async Task Invoke(CommandContext context)
        {
            var user = GetUserFromCommand(optionalUser, context);
            var data = await ethOption.Parse(context);
            if (data == null) return;

            var currentAddress = Program.UserRepo.GetCurrentAddressForUser(user);
            if (currentAddress != null && !IsSenderAdmin(context.Command))
            {
                await context.Followup($"You've already set your Ethereum address to {currentAddress}.");
                return;
            }

            var result = Program.UserRepo.AssociateUserWithAddress(user, data);
            if (result)
            {
                await context.Followup(new string[]
                {
                    "Done! Thank you for joining the test net!",
                    "By default, the bot will @-mention you with test-net reward related notifications.",
                    $"You can enable/disable this behavior with the '/{notifyCommand.Name}' command."
                });
            }
            else
            {
                await context.Followup("That didn't work.");
            }
        }
    }
}
