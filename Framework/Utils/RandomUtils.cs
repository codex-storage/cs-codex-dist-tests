﻿namespace Utils
{
    public static class RandomUtils
    {
        private static readonly Random random = new Random();
        private static readonly object @lock = new object();

        public static T GetOneRandom<T>(this T[] items)
        {
            lock (@lock)
            {
                var i = random.Next(0, items.Length);
                var result = items[i];
                return result;
            }
        }

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
                    result.Add(PickOneRandom(source));
                }
                return result.ToArray();
            }
        }

        public static string GenerateRandomString(long requiredLength)
        {
            lock (@lock)
            {
                var result = "";
                while (result.Length < requiredLength)
                {
                    var remaining = requiredLength - result.Length;
                    var len = Math.Min(1024, remaining);
                    var bytes = new byte[len];
                    random.NextBytes(bytes);
                    result += string.Join("", bytes.Select(b => b.ToString()));
                }

                return result.Substring(0, Convert.ToInt32(requiredLength));
            }
        }
    }
}
