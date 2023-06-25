namespace Utils
{
    public static class RandomUtils
    {
        private static readonly Random random = new Random();

        public static T PickOneRandom<T>(this List<T> remainingItems)
        {
            var i = random.Next(0, remainingItems.Count);
            var result = remainingItems[i];
            remainingItems.RemoveAt(i);
            return result;
        }
    }
}
