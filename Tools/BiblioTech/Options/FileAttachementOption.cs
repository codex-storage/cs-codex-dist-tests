using Discord;

namespace BiblioTech.Options
{
    public class FileAttachementOption : CommandOption
    {
        public FileAttachementOption(string name, string description, bool isRequired)
            : base(name, description, type: ApplicationCommandOptionType.Attachment, isRequired)
        {
        }

        public async Task<IAttachment?> Parse(CommandContext context)
        {
            var fileOptionData = context.Options.SingleOrDefault(o => o.Name == Name);
            if (fileOptionData == null)
            {
                await context.Command.FollowupAsync("Attachement option not received.");
                return null;
            }
            var attachement = fileOptionData.Value as IAttachment;
            if (attachement == null)
            {
                await context.Command.FollowupAsync("Attachement is null or empty.");
                return null;
            }

            return attachement;
        }
    }
}
