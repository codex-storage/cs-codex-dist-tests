namespace BiblioTech
{
    public static class RandomBusyMessage
    {
        private static readonly Random random = new Random();
        private static readonly string[] messages = new[]
        {
            "Working on it...",
            "Doing that...",
            "Hang on...",
            "Making it so...",
            "Reversing the polarity...",
            "Factoring the polynomial...",
            "Analyzing the wavelengths...",
            "Charging the flux-capacitor...",
            "Jumping to hyperspace...",
            "Computing the ultimate answer..."
        };

        public static string Get()
        {
            return messages[random.Next(messages.Length)];
        }
    }
}
