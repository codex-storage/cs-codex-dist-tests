using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class UserAssociateCommand : BaseCommand
    {
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

            // private commands

            var result = Program.UserRepo.AssociateUserWithAddress(user, data);
            if (result)
            {
                await context.Followup("Done! Thank you for joining the test net!");
            }
            else
            {
                await context.Followup("That didn't work.");
            }
        }
    }
}
