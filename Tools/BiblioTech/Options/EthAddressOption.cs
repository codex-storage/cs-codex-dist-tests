using Nethereum.Util;
using Utils;

namespace BiblioTech.Options
{
    public class EthAddressOption : CommandOption
    {
        public EthAddressOption(bool isRequired)
            : base(name: "ethaddress",
                  description: "Ethereum address starting with '0x'.",
                  type: Discord.ApplicationCommandOptionType.String,
                  isRequired)
        {
        }

        public async Task<EthAddress?> Parse(CommandContext context)
        {
            var ethOptionData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (ethOptionData == null)
            {
                await context.Followup("EthAddress option not received.");
                return null;
            }
            var ethAddressStr = ethOptionData.Value as string;
            if (string.IsNullOrEmpty(ethAddressStr))
            {
                await context.Followup("EthAddress is null or empty.");
                return null;
            }

            if (!AddressUtil.Current.IsValidAddressLength(ethAddressStr) ||
                !AddressUtil.Current.IsValidEthereumAddressHexFormat(ethAddressStr))
                // !AddressUtil.Current.IsChecksumAddress(ethAddressStr)) - this might make a good option later, but for now it might just annoy users.
            {
                await context.Followup("EthAddress is not valid.");
                return null;
            }

            return new EthAddress(ethAddressStr);
        }
    }
}
