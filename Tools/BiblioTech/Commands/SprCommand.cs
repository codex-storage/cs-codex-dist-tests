using BiblioTech.Options;
using CodexPlugin;
using Core;

namespace BiblioTech.Commands
{
    public class SprCommand : BaseCodexCommand
    {
        private readonly Random random = new Random();
        private readonly List<string> sprCache = new List<string>();
        private DateTime lastUpdate = DateTime.MinValue;

        public SprCommand(CoreInterface ci) : base(ci)
        {
        }

        public override string Name => "boot";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Gets an SPR. (Signed peer record, used for bootstrapping.)";

        protected override async Task<bool> OnInvoke(CommandContext context)
        {
            if (ShouldUpdate())
            {
                return true;
            }

            await ReplyWithRandomSpr(context);
            return false;
        }

        protected override async Task Execute(CommandContext context, ICodexNodeGroup codexGroup)
        {
            lastUpdate = DateTime.UtcNow;
            sprCache.Clear();

            var infos = codexGroup.Select(c => c.GetDebugInfo()).ToArray();
            sprCache.AddRange(infos.Select(i => i.spr));

            await ReplyWithRandomSpr(context);
        }

        private async Task ReplyWithRandomSpr(CommandContext context)
        {
            if (!sprCache.Any())
            {
                await context.Followup("I'm sorry, no SPRs are available... :c");
                return;
            }

            var i = random.Next(0, sprCache.Count);
            var spr = sprCache[i];
            await context.Followup($"Your SPR: `{spr}`");
        }

        private bool ShouldUpdate()
        {
            return (DateTime.UtcNow - lastUpdate) > TimeSpan.FromMinutes(10);
        }
    }
}
