using Discord.WebSocket;
using BiblioTech.Options;
using Discord;

namespace BiblioTech
{
    public abstract class BaseCommand
    {
        public abstract string Name { get; }
        public abstract string StartingMessage { get; }
        public abstract string Description { get; }
        public virtual CommandOption[] Options => Array.Empty<CommandOption>();

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName != Name) return;

            try
            {
                Program.Log.Log($"Responding to '{Name}'");
                var context = new CommandContext(command, command.Data.Options);
                await command.RespondAsync(StartingMessage, ephemeral: IsEphemeral(context));
                await Invoke(context);
            }
            catch (Exception ex)
            {
                var msg = "Failed with exception: " + ex;
                if (IsInAdminChannel(command))
                {
                    await command.FollowupAsync(msg.Substring(0, Math.Min(1900, msg.Length)));
                }
                else
                {
                    await command.FollowupAsync("Something failed while trying to do that...", ephemeral: true);
                    await Program.AdminChecker.GetAdminChannel().SendMessageAsync(msg);
                }
                Program.Log.Error(msg);
            }
        }

        private bool IsEphemeral(CommandContext context)
        {
            if (IsSenderAdmin(context.Command) && IsInAdminChannel(context.Command)) return false;
            return true;
        }

        protected abstract Task Invoke(CommandContext context);

        protected bool IsSenderAdmin(SocketSlashCommand command)
        {
            return Program.AdminChecker.IsUserAdmin(command.User.Id);
        }

        protected bool IsInAdminChannel(SocketSlashCommand command)
        {
            return Program.AdminChecker.IsAdminChannel(command.Channel);
        }

        protected IUser GetUserFromCommand(UserOption userOption, CommandContext context)
        {
            var targetUser = userOption.GetUser(context);
            if (IsSenderAdmin(context.Command) && targetUser != null) return targetUser;
            return context.Command.User;
        }
    }
}
