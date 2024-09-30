using BiblioTech.Options;
using Discord;
using GethPlugin;
using k8s.KubeConfigModels;
using NBitcoin.Secp256k1;

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
            var newAddress = await ethOption.Parse(context);
            if (newAddress == null) return;

            var currentAddress = Program.UserRepo.GetCurrentAddressForUser(user);
            if (currentAddress != null && !IsSenderAdmin(context.Command))
            {
                await context.Followup($"You've already set your Ethereum address to {currentAddress}.");
                await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' but already has an address set. ({currentAddress})");
                return;
            }

            var result = Program.UserRepo.AssociateUserWithAddress(user, newAddress);
            switch (result)
            {
                case SetAddressResponse.OK:
                    await ResponseOK(context, user, newAddress);
                    break;
                case SetAddressResponse.AddressAlreadyInUse:
                    await ResponseAlreadyUsed(context, user, newAddress);
                    break;
                case SetAddressResponse.CreateUserFailed:
                    await ResponseCreateUserFailed(context, user);
                    break;
                default:
                    throw new Exception("Unknown SetAddressResponse mode");
            }
        }

        private async Task ResponseCreateUserFailed(CommandContext context, IUser user)
        {
            await context.Followup("Internal error. Error details sent to admin.");
            await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' but failed to create new user.");
        }

        private async Task ResponseAlreadyUsed(CommandContext context, IUser user, EthAddress newAddress)
        {
            await context.Followup("This address is already in use by another user.");
            await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' but the provided address is already in use by another user. (address: {newAddress})");
        }

        private async Task ResponseOK(CommandContext context, IUser user, GethPlugin.EthAddress newAddress)
        {
            await context.Followup(new string[]
{
                    "Done! Thank you for joining the test net!",
                    "By default, the bot will @-mention you with test-net related notifications.",
                    $"You can enable/disable this behavior with the '/{notifyCommand.Name}' command."
});

            await Program.AdminChecker.SendInAdminChannel($"User {Mention(user)} used '/{Name}' successfully. ({newAddress})");
        }
    }
}
