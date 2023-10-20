using Discord.WebSocket;
using GethPlugin;

namespace BiblioTech.TokenCommands
{
    public class EthAddressOption : CommandOption
    {
        public EthAddressOption()
            : base(name: "ethaddress",
                  description: "Ethereum address starting with '0x'.",
                  type: Discord.ApplicationCommandOptionType.String)
        {
        }

        public async Task<EthAddress?> Parse(SocketSlashCommand command)
        {
            var ethOptionData = command.Data.Options.SingleOrDefault(o => o.Name == Name);
            if (ethOptionData == null)
            {
                await command.RespondAsync("EthAddress option not received.");
                return null;
            }
            var ethAddressStr = ethOptionData.Value as string;
            if (string.IsNullOrEmpty(ethAddressStr))
            {
                // todo, validate that it is an eth address.
                await command.RespondAsync("EthAddress is null or invalid.");
                return null;
            }

            return new EthAddress(ethAddressStr);
        }
    }
}
