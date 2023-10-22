using Discord.WebSocket;

namespace BiblioTech.TokenCommands
{
    public class UserAssociateCommand : BaseCommand
    {
        private readonly EthAddressOption ethOption = new EthAddressOption();

        public override string Name => "set";
        public override string StartingMessage => "hold on...";
        public override string Description => "Associates a Discord user with an Ethereum address in the TestNet. " +
            "Warning: You can set your Ethereum address only once! Double-check before hitting enter.";

        public override CommandOption[] Options => new[] { ethOption };

        protected override async Task Invoke(SocketSlashCommand command)
        {
            var userId = command.User.Id;
            var data = await ethOption.Parse(command);
            if (data == null) return;

            var currentAddress = Program.UserRepo.GetCurrentAddressForUser(userId);
            if (currentAddress != null)
            {
                await command.FollowupAsync($"You've already set your Ethereum address to {currentAddress}.");
                return;
            }

            Program.UserRepo.AssociateUserWithAddress(userId, data);
            await command.FollowupAsync("Done! Thank you for joining the test net!");
        }
    }
}
