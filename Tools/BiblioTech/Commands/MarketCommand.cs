using BiblioTech.Options;
using DiscordRewards;
using System.Globalization;
using Utils;

namespace BiblioTech.Commands
{
    public class MarketCommand : BaseCommand
    {
        public override string Name => "market";
        public override string StartingMessage => RandomBusyMessage.Get();
        public override string Description => "Fetch some insights about current market conditions.";

        protected override async Task Invoke(CommandContext context)
        {
            await context.Followup(GetInsights());
        }

        private string[] GetInsights()
        {
            var result = Program.Averages.SelectMany(GetInsight).ToArray();
            if (result.Length > 0)
            {
                result = new[]
                {
                    "No market insights available."
                };
            }
            return result;
        }

        private string[] GetInsight(MarketAverage avg)
        {
            var headerLine = $"[Last {Time.FormatDuration(avg.TimeRange)}] ({avg.NumberOfFinished} Contracts finished)";

            if (avg.NumberOfFinished == 0)
            {
                return new[] { headerLine }; 
            }

            return new[]
            {
                headerLine,
                $"Price: {Format(avg.Price)}",
                $"Size: {Format(avg.Size)}",
                $"Duration: {Format(avg.Duration)}",
                $"Collateral: {Format(avg.Collateral)}",
                $"ProofProbability: {Format(avg.ProofProbability)}"
            };
        }

        private string Format(float f)
        {
            return f.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
