using Discord.WebSocket;
using Discord;

namespace BiblioTech
{
    public abstract class BaseCommand
    {
        public abstract string Name { get; }
        public abstract string StartingMessage { get; }
        public abstract string Description { get; }
        public virtual CommandOption[] Options
        {
            get
            {
                return Array.Empty<CommandOption>();
            }
        }

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName != Name) return;

            try
            {
                await command.RespondAsync(StartingMessage);
                await Invoke(command);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync("Something failed while trying to do that...");
                Console.WriteLine(ex);
            }
        }

        protected abstract Task Invoke(SocketSlashCommand command);
    }

    public class CommandOption
    {
        public CommandOption(string name, string description, ApplicationCommandOptionType type)
        {
            Name = name;
            Description = description;
            Type = type;
        }

        public string Name { get; }
        public string Description { get; }
        public ApplicationCommandOptionType Type { get; }
    }
}
