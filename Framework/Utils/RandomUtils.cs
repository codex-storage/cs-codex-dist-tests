namespace Utils
{
    public static class RandomUtils
    {
        private static readonly Random random = new Random();
        private static readonly object @lock = new object();

        public static T PickOneRandom<T>(this List<T> remainingItems)
        {
            lock (@lock)
            {
                var i = random.Next(0, remainingItems.Count);
                var result = remainingItems[i];
                remainingItems.RemoveAt(i);
                return result;
            }
        }

        public static T[] Shuffled<T>(T[] items)
        {
            lock (@lock)
            {
                var result = new List<T>();
                var source = items.ToList();
                while (source.Any())
                {
                    result.Add(RandomUtils.PickOneRandom(source));
                }
                return result.ToArray();
            }
        }
    }
}
