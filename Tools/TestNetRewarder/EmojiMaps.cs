using Utils;

namespace TestNetRewarder
{
    public class EmojiMaps
    {
        private readonly string[] create = new[]
        {
            "🐟",
            "🔵",
            "🟦" // blue square
        };
        private readonly string[] positive = new[]
        {
            "🟢", // green circle
            "🟩" // green square
        };
        private readonly string[] surprise = new[]
        {
            "🧐",
            "🤨",
            "🟡", // yellow circle
            "🟨" // yellow square
        };
        private readonly string[] negative = new[]
        {
            "⛔",
            "🚫",
            "🔴",
            "🟥" // red square
        };

        public string GetCreate()
        {
            return RandomUtils.GetOneRandom(create);
        }

        public string GetPositive()
        {
            return RandomUtils.GetOneRandom(positive);
        }

        public string GetSurprise()
        {
            return RandomUtils.GetOneRandom(surprise);
        }

        public string GetNegative()
        {
            return RandomUtils.GetOneRandom(negative);
        }
    }
}
