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
            await Command.ModifyOriginalResponseAsync(m =>
            {
                m.Content = message;
            });
        }

        public async Task FollowupWithAttachement(string filename, string content)
        {
            using var fileStream = new MemoryStream();
            using var streamWriter = new StreamWriter(fileStream);
            await streamWriter.WriteAsync(content);

            await Command.FollowupWithFileAsync(fileStream, filename);
        }
    }
}
