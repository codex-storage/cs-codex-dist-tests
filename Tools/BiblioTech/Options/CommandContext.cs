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
    }
}
