namespace TranscriptAnalysis
{
    public class OccuranceMap
    {
        private readonly Dictionary<int, int> map = new Dictionary<int, int>();

        public void Add(int point)
        {
            if (map.ContainsKey(point))
            {
                map[point]++;
            }
            else
            {
                map.Add(point, 1);
            }
        }

        public void Print(Action<int, int> action)
        {
            Print(false, action);
        }

        public void PrintContinous(Action<int, int> action)
        {
            Print(true, action);
        }

        private void Print(bool continuous, Action<int, int> action)
        {
            var min = map.Keys.Min();
            var max = map.Keys.Max();

            for (var i = min; i <= max; i++)
            {
                if (map.ContainsKey(i))
                {
                    action(i, map[i]);
                }
                else if (continuous)
                {
                    action(i, 0);
                }
            }
        }
    }
}
