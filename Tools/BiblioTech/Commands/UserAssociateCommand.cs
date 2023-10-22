using Discord.WebSocket;

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
        public override string Description => "Associates a Discord user with an Ethereum address in the TestNet. " +
            "Warning: You can set your Ethereum address only once! Double-check before hitting enter.";

        public override CommandOption[] Options => new[] { ethOption };

        protected override async Task Invoke(SocketSlashCommand command)
        {
            var userId = GetUserId(optionalUser, command);
            var data = await ethOption.Parse(command);
            if (data == null) return;

            var currentAddress = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (currentAddress != null && !IsSenderAdmin(command))
            {
                await command.FollowupAsync($"You've already set your Ethereum address to {currentAddress}.");
                return;
            }

            Program.UserRepo.AssociateUserWithAddress(userId, data);
            await command.FollowupAsync("Done! Thank you for joining the test net!");
        }
    }
}
