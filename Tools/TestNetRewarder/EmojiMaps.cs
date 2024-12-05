namespace TestNetRewarder
{
    public class EmojiMaps
    {
        private readonly string[] emojis = new[]
        {
            // red
            "❤",
            "🦞",
            "🌹",
            "🍒",
            "🫖", // teapot
            "⛩",
            "🚗",
            "🔥",

            // orange
            "🧡",
            "🏀",
            "🦊",
            "🏵",
            "🍊",
            "🥕",
            "🧱",
            "🎃",

            // yellow
            "💛",
            "🌻",
            "🍋",
            "🧀",
            "🌔",
            "⭐",
            "⚡",
            "🏆",

            // green
            "💚",
            "🦎",
            "🐛",
            "🌳",
            "🍀",
            "🧩",
            "🔋",
            "♻",

            // blue
            "💙",
            "🐳",
            "♂",
            "🍉",
            "🧊",
            "🌐",
            "⚓",
            "🌀",

            // purple
            "💜",
            "🪀", //yo-yo
            "🔮",
            "😈",
            "👾",
            "🪻", // plant hyacinth
            "🍇",
            "🍆",

            // pink
            "🩷", // pink heart
            "👚",
            "♀",
            "🧠",
            "🐷",
            "🦩",
            "🌸",
            "🌷"
        };

        public string NewRequest => "🌱";
        public string Started => "🌳";
        public string SlotFilled => "🟢";
        public string SlotFreed => "⭕";
        public string SlotReservationsFull => "☑️";
        public string Finished => "✅";
        public string Cancelled => "🚫";
        public string Failed => "❌";

        public string StringToEmojis(string input, int outLength)
        {
            if (outLength < 1) outLength = 1;

            var result = "";
            var segmentLength = input.Length / outLength;
            if (segmentLength < 1)
            {
                return StringToEmojis(input + input, outLength);
            }
            for (var i = 0; i < outLength; i++)
            {
                var segment = input.Substring(i * segmentLength, segmentLength);
                result += SelectOne(segment);
            }

            return result;
        }

        private string SelectOne(string segment)
        {
            var index = 0;
            foreach (var c in segment) index += Convert.ToInt32(c);
            index = index % emojis.Length;
            return emojis[index];
        }
    }
}
