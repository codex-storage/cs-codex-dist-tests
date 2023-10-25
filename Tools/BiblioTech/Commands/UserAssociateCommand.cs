using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class UserAssociateCommand : BaseCommand
    {
        private readonly EthAddressOption ethOption = new EthAddressOption();
        private readonly UserOption optionalUser = new UserOption(
            description: "If set, associates Ethereum address for another user. (Optional, admin-only)",
            isRequired: false);

        public override string Name => "set";
        public override string StartingMessage => "hold on...";
        public override string Description => "Associates a Discord user with an Ethereum address.";
        public override CommandOption[] Options => new CommandOption[] { ethOption, optionalUser };

        protected override async Task Invoke(CommandContext context)
        {
            var userId = GetUserId(optionalUser, context);
            var data = await ethOption.Parse(context);
            if (data == null) return;

            var currentAddress = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (currentAddress != null && !IsSenderAdmin(context.Command))
            {
                await context.Command.FollowupAsync($"You've already set your Ethereum address to {currentAddress}.");
                return;
            }

            // private commands

            var result = Program.UserRepo.AssociateUserWithAddress(userId, data);
            if (result)
            {
                await context.Command.FollowupAsync("Done! Thank you for joining the test net!");
            }
            else
            {
                await context.Command.FollowupAsync("That didn't work.");
            }
        }
    }
}
