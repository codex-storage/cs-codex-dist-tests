using Discord.WebSocket;
using BiblioTech.Options;

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
                await Invoke(new CommandContext(command, command.Data.Options));
            }
            catch (Exception ex)
            {
                await command.FollowupAsync("Something failed while trying to do that...");
                Console.WriteLine(ex);
            }
        }

        protected abstract Task Invoke(CommandContext context);

        protected bool IsSenderAdmin(SocketSlashCommand command)
        {
            return Program.AdminChecker.IsUserAdmin(command.User.Id);
        }

        protected ulong GetUserId(UserOption userOption, CommandContext context)
        {
            var targetUser = userOption.GetOptionUserId(context);
            if (IsSenderAdmin(context.Command) && targetUser != null) return targetUser.Value;
            return context.Command.User.Id;
        }
    }
}
