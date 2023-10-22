using Discord.WebSocket;
using GethPlugin;
using Nethereum.Util;

namespace BiblioTech.Commands
{
    public class EthAddressOption : CommandOption
    {
        public EthAddressOption()
            : base(name: "ethaddress",
                  description: "Ethereum address starting with '0x'.",
                  type: Discord.ApplicationCommandOptionType.String,
                  isRequired: true)
        {
        }

        public async Task<EthAddress?> Parse(SocketSlashCommand command)
        {
            var ethOptionData = command.Data.Options.SingleOrDefault(o => o.Name == Name);
            if (ethOptionData == null)
            {
                await command.FollowupAsync("EthAddress option not received.");
                return null;
            }
            var ethAddressStr = ethOptionData.Value as string;
            if (string.IsNullOrEmpty(ethAddressStr))
            {
                await command.FollowupAsync("EthAddress is null or empty.");
                return null;
            }

            if (!AddressUtil.Current.IsValidAddressLength(ethAddressStr) ||
                !AddressUtil.Current.IsValidEthereumAddressHexFormat(ethAddressStr) ||
                !AddressUtil.Current.IsChecksumAddress(ethAddressStr))
            {
                await command.FollowupAsync("EthAddress is not valid.");
                return null;
            }

            return new EthAddress(ethAddressStr);
        }
    }
}
