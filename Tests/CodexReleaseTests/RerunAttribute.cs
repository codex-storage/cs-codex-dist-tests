using NUnit.Framework;

namespace CodexReleaseTests
{
    public class RerunAttribute : ValuesAttribute
    {
        private const int NumberOfReRuns = 1;

        public RerunAttribute()
        {
            var list = new List<object>();
            for (var i = 0; i < NumberOfReRuns; i++) list.Add(i);
            data = list.ToArray();
        }
    }
}
