using Discord.Commands;

namespace BiblioTech
{
    public class HelloWorldModule : ModuleBase<SocketCommandContext>
    {
        [Command("say")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
        {
            return ReplyAsync("I say: " + echo);
        }
    }
}
