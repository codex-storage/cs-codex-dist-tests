using BiblioTech.Options;

namespace BiblioTech.Commands
{
    public class SprCommand : BaseCommand
    {
        private readonly Random random = new Random();
        private readonly List<string> knownSprs = new List<string>();

        public override string Name => "boot";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Gets an SPR. (Signed peer record, used for bootstrapping.)";

        protected override async Task Invoke(CommandContext context)
        {
            await ReplyWithRandomSpr(context);
        }

        public void Add(string spr)
        {
            if (knownSprs.Contains(spr)) return;
            knownSprs.Add(spr);
        }

        public void Clear()
        {
            knownSprs.Clear();
        }

        public string[] Get()
        {
            return knownSprs.ToArray();
        }

        private async Task ReplyWithRandomSpr(CommandContext context)
        {
            if (!knownSprs.Any())
            {
                await context.Followup("I'm sorry, no SPRs are available... :c");
                return;
            }

            var i = random.Next(0, knownSprs.Count);
            var spr = knownSprs[i];
            await context.Followup($"Your SPR: `{spr}`");
        }
    }
}
