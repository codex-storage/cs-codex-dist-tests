using Discord.WebSocket;

namespace BiblioTech.Options
{
    public class CommandContext
    {
        public CommandContext(SocketSlashCommand command, IReadOnlyCollection<SocketSlashCommandDataOption> options)
        {
            Command = command;
            Options = options;
        }

        public SocketSlashCommand Command { get; }
        public IReadOnlyCollection<SocketSlashCommandDataOption> Options { get; }

        public async Task Followup(string message)
        {
            if (message.Length > 1900)
            {
                await FollowupWithAttachement("codexdiscordbot_file.txt", message);
                return;
            }

            await Command.ModifyOriginalResponseAsync(m =>
            {
                m.Content = message;
            });
        }

        private async Task FollowupWithAttachement(string filename, string content)
        {
            using var fileStream = new MemoryStream();
            using var streamWriter = new StreamWriter(fileStream);
            await streamWriter.WriteAsync(content);

            await Command.FollowupWithFileAsync(fileStream, filename);
        }
    }
}
